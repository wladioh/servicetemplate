using System;
using System.Net.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly.Caching;
using Polly.Caching.Distributed;
using Service.Infra.Network.Options;

namespace Service.Infra.Network
{
    public static class RefitResilientExtension
    {
        public static IServiceCollection AddResilientRefit<TConfig>(this IServiceCollection services,
            IConfiguration configuration, Action<ConfigureRefit<TConfig>> action) where TConfig : class
        {
            services.Configure<PollyOptions>(PollyOptions.Section, configuration);
            services.AddTransient(resolver => resolver.GetService<IOptionsMonitor<PollyOptions>>().CurrentValue);

            services.AddDistributedMemoryCache(options =>
            {
                options.SizeLimit = Int64.MaxValue;
            });
            services.AddSingleton(serviceProvider => serviceProvider
                .GetRequiredService<IDistributedCache>()
                .AsAsyncCacheProvider<string>());
            services.AddSingleton<PollyPolicyFactory>();
            var config = new ConfigureRefit<TConfig>(services);
            action?.Invoke(config);
            return services;
        }

        public static IServiceCollection ConfigureWatch<TConfiguration>(this IServiceCollection services, string sectionName,
            IConfiguration configuration) where TConfiguration : class
        {
            services.Configure<TConfiguration>(configuration.GetSection(sectionName));
            services.AddTransient(resolver => resolver.GetService<IOptionsMonitor<TConfiguration>>().CurrentValue);
            return services;
        }
    }
}
