using System;

namespace Bolt.CircuitBreaker.Abstracts
{
    public interface ICircuitRequest
    {
        string RequestId { get; }
        string CircuitKey { get; }
        TimeSpan Timeout { get; }
        int? Retry { get; }
        ICircuitContext Context { get; }
    }

    public class CircuitRequest : ICircuitRequest
    {
        public string RequestId { get; set; }

        public string CircuitKey { get; set; }

        public TimeSpan Timeout { get; set; }

        public int? Retry { get; set; }

        public ICircuitContext Context { get; private set; } = new CircuitContext();
    }
}
