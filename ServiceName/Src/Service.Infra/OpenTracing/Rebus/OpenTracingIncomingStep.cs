using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using OpenTracing;
using OpenTracing.Propagation;
using OpenTracing.Tag;
using Rebus.Messages;
using Rebus.Pipeline;

namespace Service.Infra.OpenTracing.Rebus
{
    public class OpenTracingIncomingStep : IIncomingStep
    {
        private readonly ITracer _tracer;
        private readonly string _spanName;

        public OpenTracingIncomingStep(ITracer tracer, IHostingEnvironment hosting)
        {
            _tracer = tracer;
            _spanName = $"{hosting.ApplicationName} Consumer";
        }

        private static IDisposable StartServerSpan(ITracer tracer, Dictionary<string, string> headers, string operationName)
        {
            ISpanBuilder spanBuilder;
            try
            {
                var parentSpanCtx = tracer.Extract(BuiltinFormats.TextMap, new TextMapExtractAdapter(headers));

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
            spanBuilder.WithTag(Tags.Component, headers[Headers.Type]);
            return spanBuilder.WithTag(Tags.SpanKind, Tags.SpanKindConsumer).StartActive(true);
        }

        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var message = context.Load<Message>();
            var headers = message.Headers.ToDictionary(k => k.Key, v => v.Value);

            using (var scope = StartServerSpan(_tracer, headers, _spanName))
            {

                await next();
            }
        }
    }
}
