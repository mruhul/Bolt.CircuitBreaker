using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Bolt.CircuitBreaker.Abstracts;

namespace Bolt.CircuitBreaker.PollyImpl
{
    public class EmptyCircuitBreaker : ICircuitBreaker
    {
        private readonly IEnumerable<ICircuitStatusListener> _listeners;

        public EmptyCircuitBreaker(IEnumerable<ICircuitStatusListener> listeners)
        {
            _listeners = listeners;
        }

        public async Task<ICircuitResponse> ExecuteAsync(ICircuitRequest request, Func<ICircuitRequest, Task> funcAsync)
        {
            try
            {
                var sw = Stopwatch.StartNew();

                await funcAsync(request);

                sw.Stop();

                var response = new CircuitResponse { Status = CircuitStatus.Succeed };

                await Notify(request, response, sw.Elapsed);

                return response;
            }
            catch(Exception e)
            {
                return await HandleError<CircuitResponse>(request, e);
            }
        }

        public async Task<ICircuitResponse<T>> ExecuteAsync<T>(ICircuitRequest request, Func<ICircuitRequest, Task<T>> funcAsync)
        {
            try
            {
                var sw = Stopwatch.StartNew();

                var value = await funcAsync(request);

                sw.Stop();

                var response = new CircuitResponse<T> { Status = CircuitStatus.Succeed, Value = value };

                await Notify(request, response, sw.Elapsed);

                return response;
            }
            catch (Exception e)
            {
                return await HandleError<CircuitResponse<T>>(request, e);
            }
        }

        private async Task<TResponse> HandleError<TResponse>(ICircuitRequest request, Exception e) where TResponse : ICircuitResponse, new()
        {
            CircuitBreakerLog.LogError(e, e.Message);

            var response = new TResponse { Status = CircuitStatus.Failed };

            await Notify(request, response, TimeSpan.Zero);

            return response;
        }

        private Task Notify(ICircuitRequest request, ICircuitResponse response, TimeSpan executionTime)
        {
            if (_listeners == null || !_listeners.Any()) return Task.CompletedTask;

            try
            {
                var data = new CircuitStatusData
                {
                    CircuitKey = request.CircuitKey,
                    Context = request.Context,
                    ExecutionTime = executionTime,
                    RequestId = request.RequestId,
                    Status = response.Status
                };

                var tasks = _listeners.Select(x => x.Notify(data)).ToArray();

                return Task.WhenAll(tasks);
            }
            catch (Exception e)
            {
                CircuitBreakerLog.LogError(e, e.Message);
            }

            return Task.CompletedTask;
        }
    }
}
