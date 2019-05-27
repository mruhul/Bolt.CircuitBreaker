﻿using Bolt.CircuitBreaker.Abstracts;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Bolt.CircuitBreaker.PollyImpl
{
    public class PollyCircuitBreaker : ICircuitBreaker
    {
        private readonly IPolicyProvider _policyProvider;
        private readonly IEnumerable<ICircuitStatusListener> _listeners;

        public PollyCircuitBreaker(IPolicyProvider policyProvider, IEnumerable<ICircuitStatusListener> listeners)
        {
            _policyProvider = policyProvider;
            _listeners = listeners;
        }

        public async Task<ICircuitResponse> ExecuteAsync(ICircuitRequest request, Func<ICircuitContext, Task> funcAsync)
        {
            var context = new CircuitContext(request.Context);

            var policy = await _policyProvider.Get(request, context);

            TimeSpan executionTime = TimeSpan.Zero;

            var policyResult = await policy.ExecuteAndCaptureAsync(async () => {
                var sw = Stopwatch.StartNew();
                await funcAsync(context);
                sw.Stop();
                executionTime = sw.Elapsed;
            });

            var result = BuildResponse<CircuitResponse>(request, policyResult.Outcome, policyResult.FinalException, executionTime);

            await Notify(request, result, context, executionTime);

            return result;
        }

        public async Task<ICircuitResponse<T>> ExecuteAsync<T>(ICircuitRequest request, Func<ICircuitContext, Task<T>> funcAsync)
        {
            var context = new CircuitContext(request.Context);

            var policy = await _policyProvider.Get(request, context);

            T value = default(T);
            TimeSpan executionTime = TimeSpan.Zero;

            var policyResult = await policy.ExecuteAndCaptureAsync(async () => {
                var sw = Stopwatch.StartNew();
                value = await funcAsync(context);
                sw.Stop();
                CircuitBreakerLog.LogWarning(sw.ElapsedMilliseconds.ToString());
                executionTime = sw.Elapsed;
            });

            var result = BuildResponse<CircuitResponse<T>>(request, policyResult.Outcome, policyResult.FinalException, executionTime);

            result.Value = value;

            await Notify(request, result, context, executionTime);

            return result;
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

        private TResponse BuildResponse<TResponse>(ICircuitRequest request, Polly.OutcomeType outcome, Exception finalException, TimeSpan executionTime) where TResponse : ICircuitResponse, new()
        {
            var result = new TResponse();

            if (outcome == Polly.OutcomeType.Failure)
            {
                if (finalException is Polly.Timeout.TimeoutRejectedException)
                {
                    Trace(CircuitStatus.Timeout, request, finalException, executionTime);

                    result.Status = CircuitStatus.Timeout;
                }
                else if (finalException is Polly.CircuitBreaker.BrokenCircuitException)
                {
                    Trace(CircuitStatus.Broken, request, finalException, executionTime);

                    result.Status = CircuitStatus.Broken;
                }
                else
                {
                    CircuitBreakerLog.LogError(finalException, finalException.Message);

                    Trace(CircuitStatus.Failed, request, finalException, executionTime);

                    result.Status = CircuitStatus.Failed;
                }
            }
            else
            {
                Trace(CircuitStatus.Succeed, request, finalException, executionTime);

                result.Status = CircuitStatus.Succeed;
            }

            return result;
        }

        private void Trace(CircuitStatus status, ICircuitRequest request, Exception exception, TimeSpan executiontime)
        {
            CircuitBreakerLog.LogTrace($"RequestId:{request.RequestId}|AppName:{request.AppName}|ServiceName:{request.ServiceName}|CircuitKey:{request.CircuitKey}|Status:{status}|ExecutionTime:{executiontime.TotalMilliseconds}ms|Msg:{exception?.Message}");
        }
    }

    public class CircuitException : Exception
    {
        public CircuitStatus Status { get; private set; }

        public CircuitException(string message, CircuitStatus status, Exception innerException)
            : base(message, innerException)
        {
            Status = status;
        }
    }

    public class CircuitContext : ICircuitContext
    {
        private Dictionary<string, object> _source;

        public CircuitContext()
        {
        }

        public CircuitContext(Dictionary<string, object> source)
        {
            _source = source;
        }

        public object Get(string name)
        {
            if (_source == null) return null;

            return _source.TryGetValue(name, out var result) ? result : null;
        }

        public void Set(string name, object value)
        {
            if (_source == null) _source = new Dictionary<string, object>();
            _source[name] = value;
        }
    }

    public class CircuitResponse : ICircuitResponse
    {
        public bool IsSucceed => Status == CircuitStatus.Succeed;

        public CircuitStatus Status { get; set; }
    }

    public class CircuitResponse<T> : ICircuitResponse<T>
    {
        public bool IsSucceed => Status == CircuitStatus.Succeed;

        public CircuitStatus Status { get; set; }

        public T Value { get; set; }
    }
}
