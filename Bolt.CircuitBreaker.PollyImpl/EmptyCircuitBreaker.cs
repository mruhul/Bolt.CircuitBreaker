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

        public async Task<ICircuitResponse> ExecuteAsync(ICircuitRequest request, Func<ICircuitContext, Task> funcAsync)
        {
            var context = new CircuitContext(request.Context);

            try
            {
                var sw = Stopwatch.StartNew();

                await funcAsync(context);

                sw.Stop();

                var response = new CircuitResponse { Status = CircuitStatus.Succeed };

                await Notify(request, response, context, sw.Elapsed);

                return response;
            }
            catch(Exception e)
            {
                return await HandleError<CircuitResponse>(request, e, context);
            }
        }

        public async Task<ICircuitResponse<T>> ExecuteAsync<T>(ICircuitRequest request, Func<ICircuitContext, Task<T>> funcAsync)
        {
            var context = new CircuitContext(request.Context);

            try
            {
                var sw = Stopwatch.StartNew();

                var value = await funcAsync(context);

                sw.Stop();

                var response = new CircuitResponse<T> { Status = CircuitStatus.Succeed, Value = value };

                await Notify(request, response, context, sw.Elapsed);

                return response;
            }
            catch (Exception e)
            {
                return await HandleError<CircuitResponse<T>>(request, e, context);
            }
        }

        private async Task<TResponse> HandleError<TResponse>(ICircuitRequest request, Exception e, ICircuitContext context) where TResponse : ICircuitResponse, new()
        {
            CircuitBreakerLog.LogError(e, e.Message);

            var response = new TResponse { Status = CircuitStatus.Failed };

            await Notify(request, response, context, TimeSpan.Zero);

            return response;
        }

        private Task Notify(ICircuitRequest request, ICircuitResponse response, ICircuitContext context, TimeSpan executionTime)
        {
            if (_listeners == null || !_listeners.Any()) return Task.CompletedTask;

            try
            {
                var data = new CircuitStatusData
                {
                    AppName = request.AppName,
                    CircuitKey = request.CircuitKey,
                    Context = context,
                    ExecutionTime = executionTime,
                    RequestId = request.RequestId,
                    ServiceName = request.ServiceName,
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
