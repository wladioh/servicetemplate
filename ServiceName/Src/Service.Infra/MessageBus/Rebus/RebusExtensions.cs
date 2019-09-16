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
using Rebus.Subscriptions;
using Rebus.Timeouts;
using Rebus.Transport;
using Rebus.Transport.InMem;
using Service.Infra.OpenTracing;
using Service.Infra.OpenTracing.Rebus;

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
}
