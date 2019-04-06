using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Polly.Wrap;
using Service.Infra.Network.Options;

namespace Service.Infra.Network
{
    public class PollyPolicyFactory
    {
        private readonly HttpStatusCode[] _httpStatusCodesWorthRetrying = {
            HttpStatusCode.RequestTimeout, // 408
            HttpStatusCode.InternalServerError, // 500
            HttpStatusCode.BadGateway, // 502
            HttpStatusCode.ServiceUnavailable, // 503
            HttpStatusCode.GatewayTimeout // 504
        };
        private PollyOptions _options;
        private IDictionary<string, AsyncPolicyWrap<HttpResponseMessage>>
            _policies = new ConcurrentDictionary<string, AsyncPolicyWrap<HttpResponseMessage>>();
        public PollyPolicyFactory(PollyOptions options,
            IOptionsMonitor<PollyOptions> configurationChange)
        {
            configurationChange.OnChange(ConfigurationChange_ConfigurationChanged);
            _options = options;
        }
        private void ConfigurationChange_ConfigurationChanged(PollyOptions options)
        {
            _options = options;
            _policies.Clear();
        }

        public AsyncPolicyWrap<HttpResponseMessage> CreatePolicy<T>()
        {
            var key = typeof(T).FullName;
            if (_policies.ContainsKey(key))
                return _policies[key];
            var policy = CreatePolicy(_options);
            _policies.TryAdd(key, policy);
            return policy;
        }

        private AsyncPolicyWrap<HttpResponseMessage> CreatePolicy(PollyOptions options)
        {
            var retryPolicy =
                HttpPolicyExtensions.HandleTransientHttpError()
                    .OrResult(msg => _httpStatusCodesWorthRetrying.Contains(msg.StatusCode))
                    .WaitAndRetryAsync(DecorrelatedJitter(options.Retry.MaxRetries, TimeSpan.FromMilliseconds(20),
                        TimeSpan.FromMilliseconds(options.Retry.MaxDelay)));
            var timeoutPolicy = Policy
                .TimeoutAsync(TimeSpan.FromMilliseconds(options.Timeout))
                .AsAsyncPolicy<HttpResponseMessage>();
            var circuitBreaker = Policy
                .Handle<Exception>()
                .AdvancedCircuitBreakerAsync(
                    failureThreshold: options.CircuitBreak.FailureThreshold, // Break on >=50% actions result in handled exceptions...                    
                    samplingDuration: TimeSpan.FromSeconds(options.CircuitBreak.SamplingDuration), // ... over any 10 second period
                    minimumThroughput: options.CircuitBreak.MinimumThroughput, // ... provided at least 8 actions in the 10 second period.
                    durationOfBreak: TimeSpan.FromSeconds(options.CircuitBreak.DurationOfBreak) // Break for 30 seconds.
                ).AsAsyncPolicy<HttpResponseMessage>();

            var fallback = Policy<HttpResponseMessage>
                .Handle<Exception>()
                .FallbackAsync((c) =>
                {
                    var responseMessage = new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.Gone,
                        Content = new StringContent("Please try again later")
                    };
                    return Task.FromResult(responseMessage);
                });

            return fallback.WrapAsync(circuitBreaker).WrapAsync(timeoutPolicy).WrapAsync(retryPolicy);
        }

        private static IEnumerable<TimeSpan> DecorrelatedJitter(int maxRetries, TimeSpan seedDelay, TimeSpan maxDelay)
        {
            var jitterer = new Random();
            var retries = 0;

            var seed = seedDelay.TotalMilliseconds;
            var max = maxDelay.TotalMilliseconds;
            var current = seed;

            while (++retries <= maxRetries)
            {
                current = Math.Min(max, Math.Max(seed, current * 3 * jitterer.NextDouble())); // adopting the 'Decorrelated Jitter' formula from https://www.awsarchitectureblog.com/2015/03/backoff.html.  Can be between seed and previous * 3.  Mustn't exceed max.
                yield return TimeSpan.FromMilliseconds(current);
            }
        }
    }
}
