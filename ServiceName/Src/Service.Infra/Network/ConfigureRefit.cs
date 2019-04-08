using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Polly;
using Polly.Registry;
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
                    var url = func?.Invoke(configuration);
                    client.BaseAddress = new Uri(url);
                    client.Timeout = TimeSpan.FromMilliseconds(3000);
                })
                .SetHandlerLifetime(TimeSpan.FromMinutes(5))  //Set lifetime to five minutes
                .AddHttpMessageHandler<ValidateHeaderHandler>();
            return this;
        }
    }
    public class ValidateHeaderHandler : DelegatingHandler
    {
        private readonly IAsyncPolicy<HttpResponseMessage> _policy;

        public ValidateHeaderHandler(IReadOnlyPolicyRegistry<string> policyRegistry)
        {
            _policy = policyRegistry.Get<IAsyncPolicy<HttpResponseMessage>>(DefaultPolicy.PolicyName);
        }
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return await _policy.ExecuteAsync(
                (context, token) =>  base.SendAsync(request, token), new Context(request.RequestUri.PathAndQuery), cancellationToken);
        }
    }
}
