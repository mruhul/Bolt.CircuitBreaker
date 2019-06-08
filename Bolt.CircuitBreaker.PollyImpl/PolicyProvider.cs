using Bolt.CircuitBreaker.Abstracts;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Bolt.CircuitBreaker.PollyImpl
{
    public interface IPolicyProvider
    {
        Task<IAsyncPolicy> Get(ICircuitRequest request);
    }

    public class PolicyProvider : IPolicyProvider
    {
        private static SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private static ConcurrentDictionary<string, IAsyncPolicy> _source = new ConcurrentDictionary<string, IAsyncPolicy>();
        private readonly IEnumerable<IPolicySettingsProvider> _settingsProvider;

        public PolicyProvider(IEnumerable<IPolicySettingsProvider> settingsProvider)
        {
            _settingsProvider = settingsProvider;
        }

        public async Task<IAsyncPolicy> Get(ICircuitRequest request)
        {
            IAsyncPolicy result;

            var key = $"{request.CircuitKey}:{request.Retry}:{request.Timeout}";

            if(_source.TryGetValue(key, out result))
            {
                return result;
            }

            await _semaphore.WaitAsync();

            try
            {
                if (_source.TryGetValue(key, out result))
                {
                    return result;
                }

                CircuitBreakerLog.LogTrace("Loading policy from provider...");

                PolicySettings settings = null;
                
                if(_settingsProvider != null)
                {
                    foreach(var provider in _settingsProvider)
                    {
                        settings = await provider.Get(request);

                        if (settings != null) break;
                    }
                }

                var shouldReloadSettings = settings?.ShouldReload ?? false;

                var policy = BuildPolicy(request, settings);
                
                if(!shouldReloadSettings)
                {
                    _source.TryAdd(key, policy);
                }
                else
                {
                    CircuitBreakerLog.LogWarning("Policy need to reload from provider next time as requested by provider. So escape storing the policy to reuse");
                }

                return policy;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private TimeSpan AppliedTimespan(TimeSpan? requestTimespan, TimeSpan? settingsTimespan, TimeSpan defaultValue)
        {
            var timespan = requestTimespan.HasValue
                            ? requestTimespan.Value
                            : settingsTimespan.HasValue
                                ? settingsTimespan.Value
                                : TimeSpan.Zero;

            return timespan == TimeSpan.Zero ? defaultValue : timespan;
        }

        private TimeSpan AppliedTimespan(TimeSpan? timespan, TimeSpan defaultValue)
        {
            var result = timespan.HasValue
                            ? timespan.Value
                            : TimeSpan.Zero;

            return result == TimeSpan.Zero ? defaultValue : result;
        }

        private int EmptyAlternative(int? value, int alt)
        {
            if (!value.HasValue) return alt;

            return value.Value <= 0 ? alt : value.Value;
        }

        private IAsyncPolicy BuildPolicy(ICircuitRequest request, PolicySettings settings)
        {
            settings = settings ?? new PolicySettings();

            var maxParallelization = settings.MaxParallelization ?? 100;
            var maxQueingActions = settings.MaxQueingActions ?? 10;

            var timeout = AppliedTimespan(request.Timeout, settings.Timeout, TimeSpan.FromMinutes(10));

            var retry = request.Retry ?? settings.Retry ?? 0;

            var failurePercentThresholdPercent = EmptyAlternative(settings.FailurePercentThreshold, 50);

            var failureThreshold = failurePercentThresholdPercent / 100d;

            var samplingDuration = AppliedTimespan(settings.SamplingDuration, TimeSpan.FromMilliseconds(1000));

            var minThroughput = settings.MinimumThroughput ?? 5;

            var durationOfBreak = AppliedTimespan(settings.DurationOfBreak, TimeSpan.FromMilliseconds(500));

            if(CircuitBreakerLog.IsTraceEnabled)
            {
                CircuitBreakerLog.LogTrace($@"RequestId:{request.RequestId}
                                        |AppName:{request.Context.GetAppName()}
                                        |ServiceName:{request.Context.GetServiceName()}
                                        |CircuitKey:{request.CircuitKey}
                                        |Request.Retry:{request.Retry}
                                        |Request.Timeout:{request.Timeout}
                                        |Settings.Retry:{settings.Retry}
                                        |Settings.Timeout:{settings.Timeout}
                                        |Settings.FailureThreshold:{failureThreshold}
                                        |Settings.SamplingDuration:{samplingDuration}
                                        |Settings.MinThroughput:{minThroughput}
                                        |Settings.DurationOfBreak:{durationOfBreak}
                                        |Settings.MaxParallelization:{maxParallelization}
                                        |Settings.MaxQueingActions:{maxQueingActions}");
            }

            var bulkHead = Policy.BulkheadAsync(maxParallelization, maxQueingActions);

            var timeoutPolicy = Policy.TimeoutAsync(timeout, Polly.Timeout.TimeoutStrategy.Pessimistic);

            var retryPolicy = Policy.Handle<Exception>().RetryAsync(retry);

            var circuit = Policy.Handle<Exception>()
                                .AdvancedCircuitBreakerAsync(failureThreshold,
                                        samplingDuration,
                                        minThroughput,
                                        durationOfBreak);

            return circuit.WrapAsync(bulkHead.WrapAsync(retryPolicy.WrapAsync(timeoutPolicy)));
        }
    }
}
