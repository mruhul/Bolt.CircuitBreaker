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
        Task<IAsyncPolicy> Get(ICircuitRequest request, ICircuitContext context);
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

        public async Task<IAsyncPolicy> Get(ICircuitRequest request, ICircuitContext context)
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
                        settings = await provider.Get(request, context);

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

        private IAsyncPolicy BuildPolicy(ICircuitRequest request, PolicySettings settings)
        {
            settings = settings ?? new PolicySettings();

            var maxParallelization = settings.MaxParallelization ?? 100;
            var maxQueingActions = settings.MaxQueingActions ?? 10;
            var timeout = request.Timeout == TimeSpan.Zero 
                            ? (settings.Timeout == TimeSpan.Zero 
                                ? TimeSpan.FromMinutes(5) 
                                : settings.Timeout)
                            : request.Timeout;

            var retry = request.Retry ?? settings.Retry ?? 0;

            var failureThreshold = (settings.FailurePercentThreshold ?? 50) / 100d;

            var samplingDuration = settings.SamplingDuration == TimeSpan.Zero 
                                    ? TimeSpan.FromMilliseconds(1000) 
                                    : settings.SamplingDuration;

            var minThroughput = settings.MinimumThroughput ?? 5;

            var durationOfBreak = settings.DurationOfBreak == TimeSpan.Zero 
                                    ? TimeSpan.FromMilliseconds(500) 
                                    : settings.DurationOfBreak;

            if(CircuitBreakerLog.IsTraceEnabled)
            {
                CircuitBreakerLog.LogTrace($@"RequestId:{request.RequestId}
                                        |AppName:{request.AppName}
                                        |ServiceName:{request.ServiceName}
                                        |CircuitKey:{request.CircuitKey}
                                        |Request.Retry:{(request.Retry == null ? "None": $"{request.Retry}")}
                                        |Request.Timeout:{(request.Timeout == TimeSpan.Zero ? "None" : $"{request.Timeout.TotalMilliseconds}ms")}
                                        |Settings.Retry:{retry}
                                        |Settings.Timeout:{timeout.TotalMilliseconds}ms
                                        |Settings.FailureThreshold:{failureThreshold}
                                        |Settings.SamplingDuration:{samplingDuration.TotalMilliseconds}ms
                                        |Settings.MinThroughput:{minThroughput}
                                        |Settings.DurationOfBreak:{durationOfBreak.TotalMilliseconds}ms
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
