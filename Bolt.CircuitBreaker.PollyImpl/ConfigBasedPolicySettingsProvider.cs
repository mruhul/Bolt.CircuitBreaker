using Bolt.CircuitBreaker.Abstracts;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bolt.CircuitBreaker.PollyImpl
{
    public class PolicySettingsConfig
    {
        public List<PolicySettingsUnit> Policies { get; set; }
    }

    public class PolicySettingsUnit
    {
        public string CircuitKey { get; set; }
        public int? TimeoutInMs { get; set; }
        public int? Retry { get; set; }
        public int? MaxParallelization { get; set; }
        public int? MaxQueingActions { get; set; }
        public int? FailurePercentThreshold { get; set; }
        public int? SamplingDurationInMs { get; set; }
        public int? MinimumThroughput { get; set; }
        public int? DurationOfBreakInMs { get; set; }
    }

    public class ConfigBasedPolicySettingsProvider : IPolicySettingsProvider
    {
        private readonly PolicySettingsConfig _config;

        public ConfigBasedPolicySettingsProvider(IOptions<PolicySettingsConfig> options)
        {
            _config = options.Value;
        }

        public Task<PolicySettings> Get(ICircuitRequest request)
        {
            var result = _config.Policies?.FirstOrDefault(x => string.Equals(x.CircuitKey, request.CircuitKey));

            if (result != null) return Task.FromResult(BuildSettings(result));

            var serviceName = request.Context.GetServiceName();

            result = _config.Policies?.FirstOrDefault(x => string.Equals(x.CircuitKey, $"{serviceName}"));

            if (result != null) return Task.FromResult(BuildSettings(result));

            return Task.FromResult<PolicySettings>(null);
        }

        private PolicySettings BuildSettings(PolicySettingsUnit unit)
        {
            return new PolicySettings
            {
                DurationOfBreak = GetTimestamp(unit.DurationOfBreakInMs),
                FailurePercentThreshold = unit.FailurePercentThreshold,
                MaxParallelization = unit.MaxParallelization,
                MaxQueingActions = unit.MaxQueingActions,
                MinimumThroughput = unit.MinimumThroughput,
                Retry = unit.Retry,
                SamplingDuration = GetTimestamp( unit.SamplingDurationInMs),
                ShouldReload = false,
                Timeout = GetTimestamp(unit.TimeoutInMs)
            };
        }

        private TimeSpan GetTimestamp(int? ms)
        {
            return ms.HasValue ? TimeSpan.FromMilliseconds(ms.Value) : TimeSpan.Zero;
        }
    }
}
