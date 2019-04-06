using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Rebus.Config;
using Rebus.Retry.Simple;
using Rebus.Routing;
using Rebus.Sagas;
using Rebus.Sagas.Idempotent;
using Rebus.Serialization;
using Rebus.Serialization.Json;
using Rebus.ServiceProvider;
using Rebus.Timeouts;
using Rebus.Transport;

namespace Service.Infra.MessageBus.Rebus
{
    public static class RebusExtensions
    {
        private static readonly string _directExchange = "DirectExchange";
        private static readonly string _topicExchange = "TopicsExchange";

        public static IServiceCollection AddRebus<THandler>(this IServiceCollection services, IConfiguration configuration, Action<StandardConfigurer<IRouter>> action = null)
        {
            services.Configure<MessageBusOptions>(configuration.GetSection(MessageBusOptions.Section));
            services.AddTransient(resolver => resolver.GetService<IOptionsMonitor<MessageBusOptions>>().CurrentValue);
            services.AutoRegisterHandlersFromAssemblyOf<THandler>();
            services.AddRebus(ConfigureRebus(services, action));
            services.AddScoped<IMessageBus, RebusMessageBus>();
            return services;
        }

        private static Func<RebusConfigurer, RebusConfigurer> ConfigureRebus(IServiceCollection services, Action<StandardConfigurer<IRouter>> action)
        {

            var serviceProvider = services.BuildServiceProvider();
            var rebusConfig = serviceProvider.GetService<MessageBusOptions>();
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

            void configureTransport(StandardConfigurer<ITransport> t)
            {
                if (rebusConfig.UseAzureServiceBus)
                    ConfigureAzure(t);
                else
                    ConfigureRabbit(t);
            };

            void configureLogging(RebusLoggingConfigurer l)
            {
                l.MicrosoftExtensionsLogging(serviceProvider.GetService<ILoggerFactory>());
            };

            void configureSagas(StandardConfigurer<ISagaStorage> s)
            {
                s.StoreInMongoDb(serviceProvider.GetService<IMongoDatabase>());
            };

            void configureSerialization(StandardConfigurer<ISerializer> s)
            {
                s.UseNewtonsoftJson(JsonInteroperabilityMode.PureJson);
            };

            void configureOptions(OptionsConfigurer o)
            {
                o.EnableIdempotentSagas();
                o.SetMaxParallelism(rebusConfig.MaxParallelism);
                o.SetNumberOfWorkers(rebusConfig.NumberOfWorkers);
                o.SimpleRetryStrategy(maxDeliveryAttempts: rebusConfig.Retry, errorQueueAddress: rebusConfig.ErrorQueue);
            };

            void configureTimeouts(StandardConfigurer<ITimeoutManager> t)
            {
                if (!rebusConfig.UseAzureServiceBus)
                    t.StoreInMongoDb(serviceProvider.GetService<IMongoDatabase>(), "TimeOutRebus");
            };

            return configure => configure
                   .Logging(configureLogging)
                   .Transport(configureTransport)
                   .Sagas(configureSagas)
                   .Serialization(configureSerialization)
                   .Options(configureOptions)
                   .Timeouts(configureTimeouts)
                   .Routing(r => action?.Invoke(r));
        }
    }
}
