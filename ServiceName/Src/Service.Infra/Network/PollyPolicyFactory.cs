using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Polly;
using Polly.Caching;
using Polly.Caching.Distributed;
using Polly.Caching.Serialization.Json;
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
        private readonly ILogger<HttpClient> _logger;
        private readonly IAsyncCacheProvider<string> _cacheProvider;

        private readonly ConcurrentDictionary<string, AsyncPolicyWrap<HttpResponseMessage>>
            _policies = new ConcurrentDictionary<string, AsyncPolicyWrap<HttpResponseMessage>>();
        public PollyPolicyFactory(PollyOptions options,
            IOptionsMonitor<PollyOptions> configurationChange, ILogger<HttpClient> logger,
            IAsyncCacheProvider<string> cacheProvider)
        {
            configurationChange.OnChange(ConfigurationChange_ConfigurationChanged);
            _options = options;
            _logger = logger;
            _cacheProvider = cacheProvider;
        }
        private void ConfigurationChange_ConfigurationChanged(PollyOptions options)
        {
            _options = options;
            _policies.Clear();
        }

        public AsyncPolicyWrap<HttpResponseMessage> CreatePolicy<T>()
        {
            var key = typeof(T).FullName;
            if (_policies.TryGetValue(key, out var policy))
                return policy;
            policy = CreatePolicy<T>(_options);
            return _policies.TryAdd(key, policy) ? policy : _policies[key];
        }

        private AsyncPolicyWrap<HttpResponseMessage> CreatePolicy<T>(PollyOptions options)
        {

            var retrays = DecorrelatedJitter(options.Retry.MaxRetries, TimeSpan.FromMilliseconds(20),
                TimeSpan.FromMilliseconds(options.Retry.MaxDelay));
            var retryPolicy =
                HttpPolicyExtensions.HandleTransientHttpError()
                    .OrResult(msg => _httpStatusCodesWorthRetrying.Contains(msg.StatusCode))
                    .WaitAndRetryAsync(retrays,
                        (outcome, time, retryNumber, context) =>
                        {
                            //   _logger.LogWarning("Retry "+ retryNumber);
                            return Task.CompletedTask;
                        });
            var timeoutPolicy = Policy
                .TimeoutAsync(TimeSpan.FromMilliseconds(options.Timeout),
                    (context, span, arg3) =>
                    {
                        //   _logger.LogWarning("Timeout");
                        return Task.CompletedTask;
                    })
                .AsAsyncPolicy<HttpResponseMessage>();
            var circuitBreaker = HttpPolicyExtensions.HandleTransientHttpError().Or<Exception>()
            .AdvancedCircuitBreakerAsync(
                options.CircuitBreak
                    .FailureThreshold,
                TimeSpan.FromSeconds(options.CircuitBreak
                    .SamplingDuration),
                options.CircuitBreak
                    .MinimumThroughput,
                TimeSpan.FromSeconds(options.CircuitBreak
                    .DurationOfBreak),
                (exception, span) => _logger.LogWarning("CircuitBreak Break"),
                () => _logger.LogWarning("CircuitBreak OPEN"),
                () => _logger.LogWarning("CircuitBreak HalfOpen")
            );

            var fallback = HttpPolicyExtensions.HandleTransientHttpError()
                .Or<Exception>()
                .FallbackAsync((c) =>
                {
                    var responseMessage = new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.Gone,
                        Content = new StringContent("Please try again later")
                    };
                    return Task.FromResult(responseMessage);
                });

            Func<Context, HttpResponseMessage, Ttl> cacheOnly200OKfilter =
                (context, result) =>
                {
                    return new Ttl(
                        timeSpan: result.StatusCode == HttpStatusCode.OK ? TimeSpan.FromMinutes(5) : TimeSpan.Zero,
                        slidingExpiration: true
                    );
                };
            //new 

            var serializerSettings = new JsonSerializerSettings()
            {
                // Any configuration options
                
            };
            var x = new Polly.Caching.Serialization.Json.JsonSerializer<HttpResponseMessage>(serializerSettings);
            var y = _cacheProvider.WithSerializer(new asdasd());
            var cache = Policy.CacheAsync(y,
                new ResultTtl<HttpResponseMessage>(cacheOnly200OKfilter),
                context =>
                {
                    return context.OperationKey;
                },
                (context, s) =>
                {
                    //_logger.LogInformation($"Get Cache {s}");
                },
                (context, s) => { _logger.LogWarning($"Miss Cache {s}"); },
                (context, s) =>
                {
                    //_logger.LogWarning($"Put Cache {s}");
                },
                (context, s, arg3) => { _logger.LogError($"Get Error Cache {s}"); },
                (context, s, arg3) => { _logger.LogError($"Put Error Cache {s}"); });

            var bulkhead = Policy
                .BulkheadAsync<HttpResponseMessage>(1000);

            return fallback.WrapAsync(cache)
                .WrapAsync(timeoutPolicy)
                .WrapAsync(retryPolicy)
                .WrapAsync(circuitBreaker)
                .WrapAsync(bulkhead);
        }

        private static IEnumerable<TimeSpan> DecorrelatedJitter(int maxRetries, TimeSpan seedDelay, TimeSpan maxDelay)
        {
            var jitterer = new Random(Guid.NewGuid().GetHashCode());
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

    public class asdasd: ICacheItemSerializer<HttpResponseMessage, string>{
        private JsonSerializer<HttpResponseCache> x;

        public asdasd()
        {
            var serializerSettings = new JsonSerializerSettings()
            {
                // Any configuration options

            };
            x = new Polly.Caching.Serialization.Json.JsonSerializer<HttpResponseCache>(serializerSettings);
        }
        public string Serialize(HttpResponseMessage objectToSerialize)
        {
            var asd = new HttpResponseCache
            {
                Content = objectToSerialize.Content.ReadAsStringAsync().Result,
                Headers = objectToSerialize.Content.Headers.ToArray(),
                ReasonPhrase = objectToSerialize.ReasonPhrase,
                StatusCode = objectToSerialize.StatusCode
            };
            var y = x.Serialize(asd);
            return y;
        }

        public HttpResponseMessage Deserialize(string objectToDeserialize)
        {

            var y= x.Deserialize(objectToDeserialize);
            var asd = new HttpResponseMessage
            {
                Content = new StringContent(y.Content),
                StatusCode = y.StatusCode,
                ReasonPhrase = y.ReasonPhrase
            };
            foreach (var httpContentHeader in y.Headers)
            {
                asd.Headers.TryAddWithoutValidation(httpContentHeader.Key, httpContentHeader.Value);
            }
            return asd;
        }

        public class HttpResponseCache
        {
            public string Content { get; set; }
            public string ReasonPhrase { get; set; }
            public HttpStatusCode StatusCode { get; set; }
            public KeyValuePair<string, IEnumerable<string>>[] Headers { get; set; }
        }
    }
}
