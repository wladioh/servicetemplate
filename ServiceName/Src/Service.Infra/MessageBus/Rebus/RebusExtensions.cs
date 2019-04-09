using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Rebus.Config;
using Rebus.Persistence.InMem;
using Rebus.Retry.Simple;
using Rebus.Routing;
using Rebus.Sagas;
using Rebus.Sagas.Idempotent;
using Rebus.Serialization;
using Rebus.Serialization.Json;
using Rebus.ServiceProvider;
using Rebus.Timeouts;
using Rebus.Transport;
using Rebus.Transport.InMem;

namespace Service.Infra.MessageBus.Rebus
{
    public static class RebusExtensions
    {
        private static readonly string _directExchange = "DirectExchange";
        private static readonly string _topicExchange = "TopicsExchange";

        public static IServiceCollection AddRebus<THandler>(this IServiceCollection services, IConfiguration configuration, Action<StandardConfigurer<IRouter>> action = null)
        {

            var rebusConfig = new MessageBusOptions();
            configuration.Bind(MessageBusOptions.Section, rebusConfig);
            var x = ConfigureRebus(services, rebusConfig, action);
            services.Configure<MessageBusOptions>(configuration.GetSection(MessageBusOptions.Section));
            services.AddTransient(resolver => resolver.GetService<IOptionsMonitor<MessageBusOptions>>().CurrentValue);
            services.AutoRegisterHandlersFromAssemblyOf<THandler>();

            services.AddRebus(x);
            services.AddScoped<IMessageBus, RebusMessageBus>();
            return services;
        }

        private static Func<RebusConfigurer, IServiceProvider, RebusConfigurer> ConfigureRebus(IServiceCollection services, MessageBusOptions rebusConfig,
            Action<StandardConfigurer<IRouter>> action)
        {
            void ConfigureRabbit(StandardConfigurer<ITransport> t)
            {
                t.UseRabbitMq(rebusConfig.ConnectionString, rebusConfig.Queue)
                    .ExchangeNames(_directExchange, _topicExchange)
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

            void configureTransport(StandardConfigurer<ITransport> t, IServiceProvider serviceProvider)
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

            void configureLogging(RebusLoggingConfigurer l, IServiceProvider serviceProvider)
            {
                l.MicrosoftExtensionsLogging(serviceProvider.GetService<ILoggerFactory>());
            }

            void configureSagas(StandardConfigurer<ISagaStorage> s, IServiceProvider serviceProvider)
            {
                if (rebusConfig.Transport == MessageBusOptions.TransportOptions.Memory)
                    s.StoreInMemory();
                else
                    s.StoreInMongoDb(serviceProvider.GetService<IMongoDatabase>());
            }

            void configureSerialization(StandardConfigurer<ISerializer> s, IServiceProvider serviceProvider)
            {
                s.UseNewtonsoftJson(JsonInteroperabilityMode.FullTypeInformation);
            }

            void configureOptions(OptionsConfigurer o, IServiceProvider serviceProvider)
            {
                o.EnableIdempotentSagas();
                o.SetMaxParallelism(rebusConfig.MaxParallelism);
                o.SetNumberOfWorkers(rebusConfig.NumberOfWorkers);
                o.SimpleRetryStrategy(maxDeliveryAttempts: rebusConfig.Retry, errorQueueAddress: rebusConfig.ErrorQueue);
            }

            void configureTimeouts(StandardConfigurer<ITimeoutManager> t, IServiceProvider serviceProvider)
            {
                if (rebusConfig.Transport == MessageBusOptions.TransportOptions.Memory)
                    t.StoreInMemory();
                else
                    t.StoreInMongoDb(serviceProvider.GetService<IMongoDatabase>(), "TimeOutRebus");
            }

            return (configure, provider) => configure
                   .Logging(it => configureLogging(it, provider))
                   .Transport(it => configureTransport(it, provider))
                   .Sagas(it => configureSagas(it, provider))
                   .Serialization(it => configureSerialization(it, provider))
                   .Options(it => configureOptions(it, provider))
                   .Timeouts(it => configureTimeouts(it, provider))
                   .Routing(r => action?.Invoke(r));
        }
    }
}
