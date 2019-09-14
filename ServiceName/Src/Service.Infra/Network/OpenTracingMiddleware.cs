using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Collections.Generic;
using OpenTracing;
using OpenTracing.Propagation;
using OpenTracing.Tag;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Jaeger;
using Jaeger.Samplers;
using OpenTracing.Util;

namespace Service.Api
{
    public class OpenTracingMiddleware : IMiddleware
    {
        private readonly ITracer _tracer;

        public OpenTracingMiddleware(ITracer tracer)
        {
            _tracer = tracer;
        }
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var headers = context.Request.Headers.ToDictionary(k => k.Key, v => v.Value.First());
            using (var scope = StartServerSpan(_tracer, headers, next.Method.Name))
            {
                await next(context);
            }
        }

        private static IDisposable StartServerSpan(ITracer tracer, Dictionary<string, string> headers, string operationName)
        {
            ISpanBuilder spanBuilder;
            try
            {
                var parentSpanCtx = tracer.Extract(BuiltinFormats.HttpHeaders, new TextMapExtractAdapter(headers));

                spanBuilder = tracer.BuildSpan(operationName);
                if (parentSpanCtx != null)
                {
                    spanBuilder = spanBuilder.AsChildOf(parentSpanCtx);
                }
            }
            catch (Exception)
            {
                spanBuilder = tracer.BuildSpan(operationName);
            }

            // TODO could add more tags like http.url
            return spanBuilder.WithTag(Tags.SpanKind, Tags.SpanKindServer).StartActive(true);
        }
    }

    public static class OpenTracingExtensions
    {
        public static IApplicationBuilder UseOpenTracingMiddleware(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<OpenTracingMiddleware>();
        }

        public static IServiceCollection AddOpenTracing(this IServiceCollection services)
        {
            services.AddSingleton<ITracer>(serviceProvider =>
            {
                var serviceName = serviceProvider
                    .GetRequiredService<IHostingEnvironment>()
                    .ApplicationName;

                var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

                var tracer = new Tracer.Builder(serviceName)
                    .WithSampler(new ConstSampler(true))
                    .WithLoggerFactory(loggerFactory)
                    .Build();

                // Allows code that can't use DI to also access the tracer.
                GlobalTracer.Register(tracer);

                return tracer;
            });
            return services;
        }
    }


}
