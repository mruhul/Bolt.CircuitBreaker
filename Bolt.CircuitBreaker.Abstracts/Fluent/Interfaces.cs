using System;
using System.Threading.Tasks;

namespace Bolt.CircuitBreaker.Abstracts.Fluent
{
    public interface ICircuitBreakerHaveCircuitKey : ICircuitBreakerCollectRequestId, ICircuitBreakerCollectTimeout, ICircuitBreakerExecute
    {
    }

    public interface ICircuitBreakerCollectRetry
    {
        ICircuitBreakerHaveRetry Retry(int retry);
    }

    public interface ICircuitBreakerHaveRetry : ICircuitBreakerExecute
    {

    }

    public interface ICircuitBreakerCollectRequestId
    {
        ICircuitBreakerHaveRequestId RequestId(string id);
    }

    public interface ICircuitBreakerHaveRequestId : ICircuitBreakerCollectTimeout, ICircuitBreakerExecute
    {
    }

    public interface ICircuitBreakerCollectTimeout
    {
        ICircuitBreakerHaveTimeout Timeout(TimeSpan timeSpan);
    }

    public interface ICircuitBreakerHaveTimeout : ICircuitBreakerCollectRetry, ICircuitBreakerExecute
    {

    }

    public interface ICircuitBreakerExecute
    {
        Task<ICircuitResponse<T>> ExecuteAsync<T>(Func<ICircuitRequest, Task<T>> funcAsync);
        Task<ICircuitResponse> ExecuteAsync(Func<ICircuitRequest, Task> funcAsync);
    }
}
