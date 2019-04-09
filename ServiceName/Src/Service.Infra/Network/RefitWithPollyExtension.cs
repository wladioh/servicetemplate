using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly.Caching.Distributed;
using Service.Infra.Network.Options;

namespace Service.Infra.Network
{
    public static class RefitWithPollyExtension
    {
        public static IServiceCollection AddRefitWithPolly<TConfig>(this IServiceCollection services,
            IConfiguration configuration,
            Action<ConfigureRefit<TConfig>> action) where TConfig : class
        {
           // var configuration = services.BuildServiceProvider().GetService<IConfiguration>();
            services.Configure<PollyOptions>(PollyOptions.Section, configuration);
            services.AddTransient(resolver => resolver.GetService<IOptionsMonitor<PollyOptions>>().CurrentValue);
            services.AddPolicyRegistry();
            services.AddDistributedMemoryCache();
            services.AddSingleton(serviceProvider => serviceProvider
                .GetRequiredService<IDistributedCache>()
                .AsAsyncCacheProvider<string>());
            services.AddTransient<MessageHandler>();
            services.AddSingleton<DefaultPolicy>();
            var config = new ConfigureRefit<TConfig>(services);
            action?.Invoke(config);
            return services;
        }

        public static IApplicationBuilder UseRefitWithPolly(this IApplicationBuilder app)
        {
            app.ApplicationServices.GetService<DefaultPolicy>()
                .RegisterPolicy();
            return app;
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
