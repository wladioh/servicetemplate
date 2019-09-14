using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using OpenTracing;
using OpenTracing.Propagation;
using OpenTracing.Tag;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Persistence.InMem;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Pipeline.Send;
using Rebus.Retry.Simple;
using Rebus.Routing;
using Rebus.Sagas;
using Rebus.Sagas.Idempotent;
using Rebus.Serialization;
using Rebus.Serialization.Json;
using Rebus.ServiceProvider;
using Rebus.Subscriptions;
using Rebus.Timeouts;
using Rebus.Transport;
using Rebus.Transport.InMem;

namespace Service.Infra.MessageBus.Rebus
{
    public static class RebusExtensions
    {
        private static readonly string DirectExchange = "DirectExchange";
        private static readonly string TopicExchange = "TopicsExchange";

        public static IServiceCollection AddRebus<THandler>(this IServiceCollection services, IConfiguration configuration, Action<StandardConfigurer<IRouter>> action = null)
        {

            var rebusConfig = new MessageBusOptions();
            configuration.Bind(MessageBusOptions.Section, rebusConfig);
            var configureRebus = ConfigureRebus(services, rebusConfig, action);
            services.Configure<MessageBusOptions>(configuration.GetSection(MessageBusOptions.Section));
            services.AddTransient(resolver => resolver.GetService<IOptionsMonitor<MessageBusOptions>>().CurrentValue);
            services.AutoRegisterHandlersFromAssemblyOf<THandler>();

            services.AddRebus(configureRebus);
            services.AddScoped<IMessageBus, RebusMessageBus>();
            return services;
        }

        private static Func<RebusConfigurer, IServiceProvider, RebusConfigurer> ConfigureRebus(IServiceCollection services, MessageBusOptions rebusConfig,
            Action<StandardConfigurer<IRouter>> action)
        {
            void ConfigureRabbit(StandardConfigurer<ITransport> t)
            {
                t.UseRabbitMq(rebusConfig.ConnectionString, rebusConfig.Queue)
                    .ExchangeNames(DirectExchange, TopicExchange)
                    .Prefetch(rebusConfig.Prefetch);
            }

            void ConfigureAzure(StandardConfigurer<ITransport> t)
            {
                t.UseAzureServiceBus(rebusConfig.ConnectionString, rebusConfig.Queue)
                    .AutomaticallyRenewPeekLock()
                    .EnablePrefetching(rebusConfig.Prefetch);
            }

            void ConfigureMemory(StandardConfigurer<ITransport> t, IServiceProvider serviceProvider)
            {
                t.UseInMemoryTransport(serviceProvider.GetService<InMemNetwork>(), rebusConfig.Queue);
            }

            void ConfigureTransport(StandardConfigurer<ITransport> t, IServiceProvider serviceProvider)
            {
                switch (rebusConfig.Transport)
                {
                    case MessageBusOptions.TransportOptions.Azure:
                        ConfigureAzure(t);
                        break;
                    case MessageBusOptions.TransportOptions.Rabbit:
                        ConfigureRabbit(t);
                        break;
                    case MessageBusOptions.TransportOptions.Memory:
                        ConfigureMemory(t, serviceProvider);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            void ConfigureLogging(RebusLoggingConfigurer l, IServiceProvider serviceProvider)
            {
                l.MicrosoftExtensionsLogging(serviceProvider.GetService<ILoggerFactory>());
            }

            void ConfigureSagas(StandardConfigurer<ISagaStorage> s, IServiceProvider serviceProvider)
            {
                if (rebusConfig.Transport == MessageBusOptions.TransportOptions.Memory)
                    s.StoreInMemory();
                else
                    s.StoreInMongoDb(serviceProvider.GetService<IMongoDatabase>());
            }

            void ConfigureSerialization(StandardConfigurer<ISerializer> s, IServiceProvider serviceProvider)
            {
                s.UseNewtonsoftJson(JsonInteroperabilityMode.FullTypeInformation);
            }

            void ConfigureOptions(OptionsConfigurer o, IServiceProvider serviceProvider)
            {
                o.EnableIdempotentSagas();
                o.SetMaxParallelism(rebusConfig.MaxParallelism);
                o.SetNumberOfWorkers(rebusConfig.NumberOfWorkers);
                o.SimpleRetryStrategy(maxDeliveryAttempts: rebusConfig.Retry, errorQueueAddress: rebusConfig.ErrorQueue);
                o.EnableOpenTracing(serviceProvider);
            }

            void ConfigureTimeouts(StandardConfigurer<ITimeoutManager> t, IServiceProvider serviceProvider)
            {
                if (rebusConfig.Transport == MessageBusOptions.TransportOptions.Memory)
                    t.StoreInMemory();
                else
                    t.StoreInMongoDb(serviceProvider.GetService<IMongoDatabase>(), "TimeOutRebus");
            }

            void Subscriptions(StandardConfigurer<ISubscriptionStorage> t)
            {
                if (rebusConfig.Transport == MessageBusOptions.TransportOptions.Memory)
                    t.StoreInMemory();
            }

            return (configure, provider) => configure
                   .Logging(it => ConfigureLogging(it, provider))
                   .Transport(it => ConfigureTransport(it, provider))
                   .Sagas(it => ConfigureSagas(it, provider))
                   .Serialization(it => ConfigureSerialization(it, provider))
                   .Subscriptions(Subscriptions)
                   .Options(it => ConfigureOptions(it, provider))
                   .Timeouts(it => ConfigureTimeouts(it, provider))
                   .Routing(r => action?.Invoke(r));
        }
    }

    public static class RebusOptionsOpenTracingExtensions
    {
        public static void EnableOpenTracing(this OptionsConfigurer configurer, IServiceProvider serviceProvider)
        {
            configurer.Decorate<IPipeline>(c =>
                   {
                       var tracer = serviceProvider.GetService<ITracer>();
                       var outgoingStep = new OpenTracingOutgoingStep(tracer);
                       var incomingStep = new OpenTracingIncomingStep(tracer);

                       var pipeline = c.Get<IPipeline>();

                       return new PipelineStepInjector(pipeline)
                           .OnReceive(incomingStep, PipelineRelativePosition.After, typeof(DeserializeIncomingMessageStep))
                           .OnSend(outgoingStep, PipelineRelativePosition.Before, typeof(SerializeOutgoingMessageStep));
                   });
        }
    }

    public class OpenTracingIncomingStep : IIncomingStep
    {
        private readonly ITracer _tracer;

        public OpenTracingIncomingStep(ITracer tracer)
        {
            _tracer = tracer;
        }
        private static IDisposable StartServerSpan(ITracer tracer, Dictionary<string, string> headers, string operationName)
        {
            ISpanBuilder spanBuilder;
            try
            {
                var parentSpanCtx = tracer.Extract(BuiltinFormats.HttpHeaders, new TextMapExtractAdapter(headers));

                spanBuilder = tracer.BuildSpan(operationName);
                if (parentSpanCtx != null)
                {
                    spanBuilder = spanBuilder.AsChildOf(parentSpanCtx);
                }
            }
            catch (Exception)
            {
                spanBuilder = tracer.BuildSpan(operationName);
            }

            // TODO could add more tags like http.url
            return spanBuilder.WithTag(Tags.SpanKind, Tags.SpanKindServer).StartActive(true);
        }

        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var message = context.Load<Message>();
            var headers = message.Headers.ToDictionary(k => k.Key, v => v.Value);

            using (var scope = StartServerSpan(_tracer, headers, next.Method.Name))
                await next();
        }
    }

    public class OpenTracingOutgoingStep : IOutgoingStep
    {
        private readonly ITracer _tracer;

        public OpenTracingOutgoingStep(ITracer tracer)
        {
            _tracer = tracer;
        }
        public async Task Process(OutgoingStepContext context, Func<Task> next)
        {
            var message = context.Load<Message>();
            var headers = message.Headers;
            var destinationAddressesList = context.Load<DestinationAddresses>().ToList();
            using (_tracer.BuildSpan("Send Message").StartActive(finishSpanOnDispose: true))
            {
                var span = _tracer.ScopeManager.Active.Span
                       .SetTag(Tags.SpanKind, Tags.SpanKindClient);
                destinationAddressesList.ForEach(it => span.SetTag(Tags.MessageBusDestination, it));
                var dictionary = new Dictionary<string, string>();
                _tracer.Inject(span.Context, BuiltinFormats.TextMap, new TextMapInjectAdapter(dictionary));

                foreach (var entry in dictionary)
                    headers.Add(entry.Key, entry.Value);
                await next();
            }
        }
    }
}
