using System;
using System.Threading.Tasks;

namespace Bolt.CircuitBreaker.Abstracts
{
    public interface ICircuitStatusListener
    {
        Task Notify(ICircuitStatusData statusData);
    }

    public interface ICircuitStatusData
    {
        TimeSpan ExecutionTime { get; }
        string RequestId { get; }
        string AppName { get;  }
        string ServiceName { get;  }
        string CircuitKey { get; }
        CircuitStatus Status { get; }
        ICircuitContext Context { get; set; }
    }
}
