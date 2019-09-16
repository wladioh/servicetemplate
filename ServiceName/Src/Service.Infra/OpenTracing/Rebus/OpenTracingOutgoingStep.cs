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
using Rebus.Pipeline.Send;

namespace Service.Infra.OpenTracing.Rebus
{
    public class OpenTracingOutgoingStep : IOutgoingStep
    {
        private readonly ITracer _tracer;
        private readonly string _spanName;

        public OpenTracingOutgoingStep(ITracer tracer, IHostingEnvironment hosting)
        {
            _tracer = tracer;
            _spanName = $"{hosting.ApplicationName} Producer";
        }

        public async Task Process(OutgoingStepContext context, Func<Task> next)
        {
            var message = context.Load<Message>();
            var headers = message.Headers;
            var destinationAddressesList = context.Load<DestinationAddresses>().ToList();
            using (_tracer.BuildSpan(_spanName).StartActive(true))
            {
                var span = _tracer.ScopeManager.Active.Span
                    .SetTag(Tags.SpanKind, Tags.SpanKindProducer);
                span.SetTag(Tags.Component, headers[Headers.Type]);
                destinationAddressesList.ForEach(it => span.SetTag(Tags.MessageBusDestination, it));
                var dictionary = new Dictionary<string, string>();
                _tracer.Inject(span.Context, BuiltinFormats.TextMap, new TextMapInjectAdapter(dictionary));

                foreach (var entry in dictionary)
                    headers.Add(entry.Key, entry.Value);
                await next();
            }
        }
    }
}
