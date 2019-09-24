using System;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Winton.Extensions.Configuration.Consul;

namespace Service.Infra.ConfigurationService
{
    public static class ConsulExtensions
    {
        private static readonly CancellationTokenSource CancellationToken = new CancellationTokenSource();
        public static IHostBuilder  ConfigureConsul(this IHostBuilder  webHostBuilder)
        {
            webHostBuilder.ConfigureAppConfiguration((context, configurationBuilder) =>
            {
                var configuration = configurationBuilder.Build();
                var consulConfiguration = new ServiceConfigurationOptions();
                configuration.GetSection(ServiceConfigurationOptions.SectionName).Bind(consulConfiguration);
                configurationBuilder.AddConsul(consulConfiguration.KeyName, CancellationToken.Token, config =>
                {
                    config.Optional = consulConfiguration.Optional;
                    config.ReloadOnChange = true;
                    config.ConsulConfigurationOptions = configurationClient =>
                        {
                            configurationClient.Address = new Uri(consulConfiguration.ConnectionString);
                            configurationClient.WaitTime = TimeSpan.FromSeconds(5);
                        };
                });
            });
            return webHostBuilder;
        }
        public static IApplicationBuilder UseConsul(this IApplicationBuilder app)
        {
            var lifeTime = app.ApplicationServices.GetService<IHostApplicationLifetime>();
            lifeTime.ApplicationStopping.Register(CancellationToken.Cancel);
            return app;
        }
    }
}
