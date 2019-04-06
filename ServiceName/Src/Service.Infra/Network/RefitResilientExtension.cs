using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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