using System;
using System.Collections.Generic;

namespace Bolt.CircuitBreaker.Abstracts
{
    public interface ICircuitRequest
    {
        string RequestId { get; }
        string AppName { get; }
        string ServiceName { get; }
        string CircuitKey { get; }
        TimeSpan Timeout { get; }
        int? Retry { get; }
        Dictionary<string,object> Context { get; }
    }

    public class CircuitRequest : ICircuitRequest
    {
        public string RequestId { get; set; }

        public string AppName { get; set; }

        public string ServiceName { get; set; }

        public string CircuitKey { get; set; }

        public TimeSpan Timeout { get; set; }

        public int? Retry { get; set; }

        public Dictionary<string, object> Context { get; set; }
    }
}
