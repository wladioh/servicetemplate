using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Bulkhead;
using Polly.Caching;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Fallback;
using Polly.Registry;
using Polly.Retry;
using Service.Infra.Network.Options;

namespace Service.Infra.Network
{
    public class DefaultPolicy
    {
        private PollyOptions _options;
        private readonly ILogger<HttpClient> _logger;
        private readonly AsyncSerializingCacheProvider<HttpResponseMessage, string> _cacheProvider;
        private readonly IPolicyRegistry<string> _policyRegistry;
        public static string PolicyName = nameof(DefaultPolicy);
        public DefaultPolicy(PollyOptions options,
            IOptionsMonitor<PollyOptions> configurationChange, ILogger<HttpClient> logger,
            IAsyncCacheProvider<string> cacheProvider, IPolicyRegistry<string> policyRegistry)
        {
            configurationChange.OnChange(ConfigurationChange_ConfigurationChanged);
            _options = options;
            _logger = logger;
            _cacheProvider = cacheProvider.WithSerializer(new HttpResponseCacheSerializer());
            _policyRegistry = policyRegistry;
        }
        private void ConfigurationChange_ConfigurationChanged(PollyOptions options)
        {
            _options = options;
            _policyRegistry[PolicyName] = CreatePolicy();
        }

        public void RegisterPolicy()
        {
            _policyRegistry[PolicyName] = CreatePolicy();
        }

        private IAsyncPolicy<HttpResponseMessage> CreatePolicy()
        {

            var retryPolicy = BuildRetryPolicy();
            var timeoutPolicy = TimeoutPolicy();
            var circuitBreaker = CircuitBreakerPolicy();
            var fallback = FallbackPolicy();
            var cache = CachePolicy();
            var bulkhead = BulkheadPolicy();

            return fallback.WrapAsync(cache)
                .WrapAsync(timeoutPolicy)
                .WrapAsync(retryPolicy)
                .WrapAsync(circuitBreaker)
                .WrapAsync(bulkhead);
        }

        private AsyncBulkheadPolicy<HttpResponseMessage> BulkheadPolicy()
        {
            var bulkhead = Policy
                .BulkheadAsync<HttpResponseMessage>(_options.Bulkhead.MaxParallelization,
                    _options.Bulkhead.MaxQueuingActions);
            return bulkhead;
        }

        private AsyncCachePolicy<HttpResponseMessage> CachePolicy()
        {
            Ttl cacheOnly200OKfilter(Context context, HttpResponseMessage result)
            {
                return new Ttl(
                    timeSpan: result.StatusCode == HttpStatusCode.OK ? TimeSpan.FromMinutes(_options.Cache.TimeSpan) : TimeSpan.Zero,
                    slidingExpiration: true
                );
            }

            var cache = Policy.CacheAsync(_cacheProvider,
                new ResultTtl<HttpResponseMessage>(cacheOnly200OKfilter),
                context => context.OperationKey,
                (context, s) => { },
                onCacheMiss,
                (context, s) => { },
                onCacheGetError: onGetError,
                onCachePutError: onPutError);
            return cache;
            void onCacheMiss(Context context, string key)
            {
                _logger.LogInformation("[Cache Policy][Miss] key: {operationKey} - id: {id} - cacheKey: {key}",
                    context.OperationKey, context.CorrelationId, key);
            }
            void onGetError(Context context, string key, Exception ex)
            {
                _logger.LogError(ex, "[Cache Policy][Get] key: {operationKey} - id: {id} - cacheKey: {key}",
                    context.OperationKey, context.CorrelationId, key);
            }
            void onPutError(Context context, string key, Exception ex)
            {
                _logger.LogError(ex, "[Cache Policy][Put] key: {operationKey} - id: {id} - cacheKey: {key}",
                    context.OperationKey, context.CorrelationId, key);
            }
        }

        private AsyncFallbackPolicy<HttpResponseMessage> FallbackPolicy()
        {
            Task OnFallbackAsync(DelegateResult<HttpResponseMessage> delegateResylt, Context context)
            {
                _logger.LogInformation("[Fallback Policy] key: {operationKey} - id: {id}",
                    context.OperationKey,
                    context.CorrelationId);
                return Task.CompletedTask;
            }

            var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Gone,
                Content = new StringContent("Please try again later")
            };

            var fallback = HttpPolicyExtensions.HandleTransientHttpError()
                .FallbackAsync(responseMessage, onFallbackAsync: OnFallbackAsync);
            return fallback;
        }

        private AsyncCircuitBreakerPolicy<HttpResponseMessage> CircuitBreakerPolicy()
        {
            var durationOfBreak = TimeSpan.FromSeconds(_options.CircuitBreak.DurationOfBreak);
            var samplingDuration = TimeSpan.FromSeconds(_options.CircuitBreak.SamplingDuration);
            var circuitBreaker = HttpPolicyExtensions.HandleTransientHttpError().Or<Exception>()
                .AdvancedCircuitBreakerAsync(
                    _options.CircuitBreak.FailureThreshold, samplingDuration,
                    _options.CircuitBreak.MinimumThroughput, durationOfBreak,
                    onBreak, onOpen);
            return circuitBreaker;
            void onBreak(DelegateResult<HttpResponseMessage> delegateResult, TimeSpan span, Context context)
            {
                _logger.LogWarning("[CircuitBreak Policy][Close] key: {operationKey} - id: {id}",
                    context.OperationKey, context.CorrelationId);
            }
            void onOpen(Context context)
            {
                _logger.LogInformation("[CircuitBreak Policy][Open] key: {operationKey} - id: {id}",
                    context.OperationKey, context.CorrelationId);
            }
        }

        private IAsyncPolicy<HttpResponseMessage> TimeoutPolicy()
        {
            Task CompletedTask(Context context, TimeSpan span, Task task)
            {
                _logger.LogInformation("[Timeout Policy] key: {operationKey} - id: {id} - timeout: {totalMilliseconds}",
                    context.OperationKey, context.CorrelationId, span.TotalMilliseconds);
                return Task.CompletedTask;
            }

            var timeout = TimeSpan.FromMilliseconds(_options.Timeout);
            var timeoutPolicy = Policy
                .TimeoutAsync(timeout, CompletedTask)
                .AsAsyncPolicy<HttpResponseMessage>();
            return timeoutPolicy;
        }

        private AsyncRetryPolicy<HttpResponseMessage> BuildRetryPolicy()
        {
            Task CompletedTask(DelegateResult<HttpResponseMessage> outcome, TimeSpan time, int retryNumber, Context context)
            {
                _logger.LogInformation("[Retry Policy] key: {operationKey} - id: {id} - retries: {retryNumber}",
                    context.OperationKey, context.CorrelationId, retryNumber);
                return Task.CompletedTask;
            }

            var sleepDurations = GenerateJitter(_options.Retry.MaxRetries, TimeSpan.FromMilliseconds(20),
                TimeSpan.FromMilliseconds(_options.Retry.MaxDelay));
            var retryPolicy =
                HttpPolicyExtensions.HandleTransientHttpError()
                    .WaitAndRetryAsync(sleepDurations, CompletedTask);
            return retryPolicy;
        }

        private static IEnumerable<TimeSpan> GenerateJitter(int maxRetries, TimeSpan seedDelay, TimeSpan maxDelay)
        {
            var jitterer = new Random(Guid.NewGuid().GetHashCode());
            var retries = 0;

            var seed = seedDelay.TotalMilliseconds;
            var max = maxDelay.TotalMilliseconds;
            var current = seed;

            while (++retries <= maxRetries)
            {
                // adopting the 'Decorrelated Jitter' formula from https://www.awsarchitectureblog.com/2015/03/backoff.html.  Can be between seed and previous * 3.  Mustn't exceed max.
                current = Math.Min(max, Math.Max(seed, current * 3 * jitterer.NextDouble()));
                yield return TimeSpan.FromMilliseconds(current);
            }
        }
    }
}
