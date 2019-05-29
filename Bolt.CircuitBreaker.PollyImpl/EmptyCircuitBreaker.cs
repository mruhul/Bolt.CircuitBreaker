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
            var (appName, serviceName) = request.SetupDefaultContext();

            try
            {
                var sw = Stopwatch.StartNew();

                await funcAsync(request);

                sw.Stop();

                var response = new CircuitResponse { Status = CircuitStatus.Succeed };

                await Notify(appName, serviceName, request, response, sw.Elapsed);

                return response;
            }
            catch(Exception e)
            {
                return await HandleError<CircuitResponse>(appName, serviceName, request, e);
            }
        }

        public async Task<ICircuitResponse<T>> ExecuteAsync<T>(ICircuitRequest request, Func<ICircuitRequest, Task<T>> funcAsync)
        {
            var (appName, serviceName) = request.SetupDefaultContext();

            try
            {
                var sw = Stopwatch.StartNew();

                var value = await funcAsync(request);

                sw.Stop();

                var response = new CircuitResponse<T> { Status = CircuitStatus.Succeed, Value = value };

                await Notify(appName, serviceName, request, response, sw.Elapsed);

                return response;
            }
            catch (Exception e)
            {
                return await HandleError<CircuitResponse<T>>(appName, serviceName, request, e);
            }
        }

        private async Task<TResponse> HandleError<TResponse>(string appName, string serviceName, ICircuitRequest request, Exception e) where TResponse : ICircuitResponse, new()
        {
            CircuitBreakerLog.LogError(e, e.Message);

            var response = new TResponse { Status = CircuitStatus.Failed };

            await Notify(appName, serviceName, request, response, TimeSpan.Zero);

            return response;
        }

        private Task Notify(string appName, string serviceName, ICircuitRequest request, ICircuitResponse response, TimeSpan executionTime)
        {
            if (_listeners == null || !_listeners.Any()) return Task.CompletedTask;

            try
            {
                var data = new CircuitStatusData
                {
                    AppName = appName,
                    CircuitKey = request.CircuitKey,
                    Context = request.Context,
                    ExecutionTime = executionTime,
                    RequestId = request.RequestId,
                    ServiceName = serviceName,
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
