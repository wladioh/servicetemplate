using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using OpenTracing;
using OpenTracing.Propagation;
using OpenTracing.Tag;
using Polly;
using Polly.Registry;

namespace Service.Infra.Network
{
    public class MessageHandler : DelegatingHandler
    {
        private readonly IAsyncPolicy<HttpResponseMessage> _policy;
        private readonly ITracer _tracer;

        public MessageHandler(IReadOnlyPolicyRegistry<string> policyRegistry, ITracer tracer)
        {
            _policy = policyRegistry.Get<IAsyncPolicy<HttpResponseMessage>>(DefaultPolicy.PolicyName);
            _tracer = tracer;
        }
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            //todo: move this code to an interface on opentracing's namespace 
            using (_tracer.BuildSpan(request.RequestUri.LocalPath).StartActive(finishSpanOnDispose: true))
            {
                var span = _tracer.ScopeManager.Active.Span
                       .SetTag(Tags.SpanKind, Tags.SpanKindClient)
                       .SetTag(Tags.HttpMethod, request.Method.Method)
                       .SetTag(Tags.HttpUrl, request.RequestUri.ToString());

                var dictionary = new Dictionary<string, string>();
                _tracer.Inject(span.Context, BuiltinFormats.HttpHeaders, new TextMapInjectAdapter(dictionary));

                foreach (var entry in dictionary)
                    request.Headers.Add(entry.Key, entry.Value);
                return await _policy.ExecuteAsync(
                    (context, token) => base.SendAsync(request, token), new Context(request.RequestUri.PathAndQuery), cancellationToken);
            }
        }
    }
}
