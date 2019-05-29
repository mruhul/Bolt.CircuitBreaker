using System;
using System.Threading.Tasks;

namespace Bolt.CircuitBreaker.Abstracts
{
    public interface ICircuitBreaker
    {
        Task<ICircuitResponse> ExecuteAsync(ICircuitRequest request, Func<ICircuitRequest, Task> funcAsync);
        Task<ICircuitResponse<T>> ExecuteAsync<T>(ICircuitRequest request, Func<ICircuitRequest, Task<T>> funcAsync);
    }
}
