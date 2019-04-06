using System;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Refit;

namespace Service.Infra.Network.Options
{
    public class ConfigureRefit<TConfiguration>
    {
        private readonly IServiceCollection _services;
        private readonly RefitSettings _settings;

        public ConfigureRefit(IServiceCollection services)
        {
            _services = services;
            var serializer = new JsonContentSerializer(new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                }
            });
            _settings = new RefitSettings
            {
                ContentSerializer = serializer
            };
        }
        public ConfigureRefit<TConfiguration> Configure<T>(Func<TConfiguration, string> func)
            where T : class
        {
            var provider = _services.BuildServiceProvider();
            _services.AddRefitClient<T>(_settings)
                .ConfigureHttpClient(client =>
                {
                    var configuration = provider.GetService<TConfiguration>();
                    var url = func?.Invoke(configuration);
                    client.BaseAddress = new Uri(url);
                })
                .SetHandlerLifetime(TimeSpan.FromMinutes(5))  //Set lifetime to five minutes
                .AddPolicyHandler((serviceProvider, request) =>
                {
                    var factory = serviceProvider.GetService<PollyPolicyFactory>();
                    return factory.CreatePolicy<T>();
                });
            return this;
        }
    }
}
