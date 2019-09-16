using System;
using Jaeger;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTracing;
using OpenTracing.Contrib.NetCore.CoreFx;
using OpenTracing.Util;

namespace Service.Infra.OpenTracing
{
    public static class OpenTracingExtensions
    {
        private static readonly Uri _jaegerUri = new Uri("http://localhost:8500/v1/kv/ServiceName");
        public static IServiceCollection AddOpenTracingJaeger(this IServiceCollection services)
        {
            services.AddOpenTracing();

            services.AddSingleton<ITracer>(serviceProvider =>
            {
                var serviceName = serviceProvider
                    .GetRequiredService<IHostingEnvironment>()
                    .ApplicationName;

                var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
                Environment.SetEnvironmentVariable("JAEGER_SERVICE_NAME", serviceName);
                Environment.SetEnvironmentVariable("JAEGER_AGENT_HOST", "localhost"); //todo: configurar no config.json
                Environment.SetEnvironmentVariable("JAEGER_AGENT_PORT", "6831");
                Environment.SetEnvironmentVariable("JAEGER_SAMPLER_TYPE", "const");
                var config = Configuration.FromEnv(loggerFactory);

                var tracer = config.GetTracerBuilder().Build();
                //var tracer = new Tracer.Builder(serviceName)
                //    .WithSampler( new ConstSampler(true))
                //    .WithLoggerFactory(loggerFactory)
                //    .Build();                
                // Allows code that can't use DI to also access the tracer.
                GlobalTracer.Register(tracer);

                return tracer;
            });

            services.Configure<HttpHandlerDiagnosticOptions>(options =>
            {
                options.IgnorePatterns.Add(request => _jaegerUri.IsBaseOf(request.RequestUri));
            });
            return services;
        }
    }
}
