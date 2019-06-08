using Bolt.CircuitBreaker.Abstracts;
using System;
using System.Threading.Tasks;

namespace Bolt.CircuitBreaker.PollyImpl
{
    public interface IPolicySettingsProvider
    {
        Task<PolicySettings> Get(ICircuitRequest request);
    }

    public class PolicySettings
    {
        public bool ShouldReload { get; set; }
        public TimeSpan? Timeout { get; set; }
        public int? Retry { get; set; }
        public int? MaxParallelization { get; set; }
        public int? MaxQueingActions { get; set; }
        public int? FailurePercentThreshold { get; set; }
        public TimeSpan? SamplingDuration { get; set; }
        public int? MinimumThroughput { get; set; }
        public TimeSpan? DurationOfBreak { get; set; }
    }
}
