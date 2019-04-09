using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.Registry;

namespace Service.Infra.Network
{
    public class MessageHandler : DelegatingHandler
    {
        private readonly IAsyncPolicy<HttpResponseMessage> _policy;

        public MessageHandler(IReadOnlyPolicyRegistry<string> policyRegistry)
        {
            _policy = policyRegistry.Get<IAsyncPolicy<HttpResponseMessage>>(DefaultPolicy.PolicyName);
        }
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return await _policy.ExecuteAsync(
                (context, token) =>  base.SendAsync(request, token), new Context(request.RequestUri.PathAndQuery), cancellationToken);
        }
    }
}