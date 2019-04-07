using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Polly;
using Refit;

namespace Service.Infra.Network
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
            _services.AddRefitClient<T>(_settings)
                .ConfigureHttpClient((provider, client) =>
                {
                    var configuration = provider.GetService<TConfiguration>();
                    var url = func?.Invoke(configuration);
                    client.BaseAddress = new Uri(url);
                    client.Timeout = TimeSpan.FromMilliseconds(300);
                })
                .SetHandlerLifetime(TimeSpan.FromMinutes(5))  //Set lifetime to five minutes
                //.AddPolicyHandler((serviceProvider, request) =>
                //{
                //    var factory = serviceProvider.GetService<PollyPolicyFactory>();
                //    var context = new Context("CacheKeyToUseWithThisRequest");
                //    context.Add("ID", "CacheKeyToUseWithThisRequest");
                //    request.SetPolicyExecutionContext(context);
                //    return factory.CreatePolicy<T>();
                //})
                .AddHttpMessageHandler(provider =>
                {
                    var factory = provider.GetService<PollyPolicyFactory>();                    
                    return new ValidateHeaderHandler(factory.CreatePolicy<T>());
                });
            return this;
        }
    }
    public class ValidateHeaderHandler : DelegatingHandler
    {
        private readonly IAsyncPolicy<HttpResponseMessage> _policy;

        public ValidateHeaderHandler(IAsyncPolicy<HttpResponseMessage> policy)
        {
            _policy = policy;
        }
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {;
            return await _policy.ExecuteAsync(
                (context, token) => base.SendAsync(request, token), new Context(request.RequestUri.PathAndQuery),  cancellationToken);
        }
    }
}
