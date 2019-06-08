using System;
using System.Threading.Tasks;
using Bolt.CircuitBreaker.Abstracts;

namespace Bolt.CircuitBreaker.PollyImpl
{
    public class ContextBasedPolicySettingsProvider : IPolicySettingsProvider
    {
        public Task<PolicySettings> Get(ICircuitRequest request)
        {
            var settings = new PolicySettings();
            
            settings.DurationOfBreak = GetTimeSpan(request, "Polly.DurationOfBreak");
            settings.FailurePercentThreshold = GetInt(request, "Polly.FailurePercentThreshold");
            settings.MaxParallelization = GetInt(request, "Polly.MaxParallelization");
            settings.MaxQueingActions = GetInt(request, "Polly.MaxQueingActions");
            settings.MinimumThroughput = GetInt(request, "Polly.MinimumThroughput");
            settings.SamplingDuration = GetTimeSpan(request, "Polly.SamplingDuration");
            settings.Timeout = GetTimeSpan(request, "Polly.Timeout");
            settings.Retry = GetInt(request, "Polly.Retry");

            return Task.FromResult(settings);
        }

        private int? GetInt(ICircuitRequest request, string key)
        {
            var strValue = request.Context.Get<string>(key);

            if (string.IsNullOrWhiteSpace(strValue)) return null;

            return int.TryParse(strValue, out var result) ? result : (int?)null;
        }

        private TimeSpan? GetTimeSpan(ICircuitRequest request, string key)
        {
            var value = GetInt(request, key);
            return value.HasValue ? TimeSpan.FromMilliseconds(value.Value) : (TimeSpan?)null;
        }
    }
}
