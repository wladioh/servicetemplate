using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Refit;
using Service.Infra.Network.Options;

namespace Service.Infra.Network
{
    public class ConfigureRefit<TConfiguration>
    {
        private readonly IServiceCollection _services;
        private readonly RefitSettings _settings;

        public ConfigureRefit(IServiceCollection services)
        {
            _services = services;
            var serializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                }
            };
            var serializer = new JsonContentSerializer(serializerSettings);
            _settings = new RefitSettings
            {
                ContentSerializer = serializer                
            };
        }
        public ConfigureRefit<TConfiguration> Configure<T>(Func<TConfiguration, string> func)
            where T : class
        {
            _services.AddRefitClient<T>(_settings)
                .ConfigureHttpClient((provider, client) =>
                {
                    var configuration = provider.GetService<TConfiguration>();
                    var pollyOptions = provider.GetService<PollyOptions>();
                    var url = func?.Invoke(configuration);
                    client.BaseAddress = new Uri(url);
                 //   client.Timeout = TimeSpan.FromMilliseconds(pollyOptions.Timeout *2);
                    client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
                })
                .SetHandlerLifetime(TimeSpan.FromMinutes(5))  //Set lifetime to five minutes     
                //.AddPolicyHandlerFromRegistry(DefaultPolicy.PolicyName) //not used because of the "Opentrancing" implementation in MessageHanlder that cause an error related with Span Injection
                .ConfigureHttpMessageHandlerBuilder(c =>
                {
                    c.PrimaryHandler = new HttpClientHandler()
                    {
                        AutomaticDecompression = System.Net.DecompressionMethods.GZip                        
                    };
                })
                .AddHttpMessageHandler<MessageHandler>();
            return this;
        }
    }
}
