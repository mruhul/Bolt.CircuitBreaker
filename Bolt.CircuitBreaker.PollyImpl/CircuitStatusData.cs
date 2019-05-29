using Bolt.CircuitBreaker.Abstracts;
using System;

namespace Bolt.CircuitBreaker.PollyImpl
{
    internal class CircuitStatusData : ICircuitStatusData
    {
        public TimeSpan ExecutionTime { get; set; }

        public string RequestId { get; set; }

        public string AppName { get; set; }

        public string ServiceName { get; set; }

        public string CircuitKey { get; set; }

        public CircuitStatus Status { get; set; }

        public ICircuitContext Context { get; set; }
    }
}
