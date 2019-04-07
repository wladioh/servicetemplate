using System;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Winton.Extensions.Configuration.Consul;

namespace Service.Infra.ConfigurationService
{
    public static class ConsulExtensions
    {
        private static readonly CancellationTokenSource _cancellationToken = new CancellationTokenSource();
        public static IWebHostBuilder ConfigureConsul(this IWebHostBuilder webHostBuilder)
        {
            webHostBuilder.ConfigureAppConfiguration((context, configurationBuilder) =>
            {
                var configuration = configurationBuilder.Build();
                var consulConfiguration = new ServiceConfigurationOptions();
                configuration.GetSection(ServiceConfigurationOptions.SectioName).Bind(consulConfiguration);
                configurationBuilder.AddConsul(consulConfiguration.KeyName, _cancellationToken.Token, config =>
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
            var lifeTime = app.ApplicationServices.GetService<IApplicationLifetime>();
            lifeTime.ApplicationStopping.Register(_cancellationToken.Cancel);
            return app;
        }
    }
}
