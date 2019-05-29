using System;
using System.Threading.Tasks;

namespace Bolt.CircuitBreaker.Abstracts.Fluent
{
    internal class FluentCircuitBreaker 
        : ICircuitBreakerHaveCircuitKey, 
        ICircuitBreakerExecute, 
        ICircuitBreakerHaveTimeout, 
        ICircuitBreakerHaveRetry, 
        ICircuitBreakerHaveRequestId
    {
        private readonly ICircuitBreaker _circuitBreaker;
        private readonly string _circuitKey;
        private readonly string _appName;
        private readonly string _serviceName;
        private TimeSpan _timeout;
        private int? _retry;
        private string _requestId;

        internal FluentCircuitBreaker(ICircuitBreaker circuitBreaker, string circuitKey, string appName, string serviceName)
        {
            _circuitBreaker = circuitBreaker;
            _circuitKey = circuitKey;
            _appName = appName;
            _serviceName = serviceName;
        }

        public Task<ICircuitResponse<T>> ExecuteAsync<T>(Func<ICircuitRequest, Task<T>> funcAsync)
        {
            return _circuitBreaker.ExecuteAsync(BuildRequest(), funcAsync);
        }

        public Task<ICircuitResponse> ExecuteAsync(Func<ICircuitRequest, Task> funcAsync)
        {
            return _circuitBreaker.ExecuteAsync(BuildRequest(), funcAsync);
        }


        public ICircuitBreakerHaveRequestId RequestId(string id)
        {
            _requestId = id;
            return this;
        }

        public ICircuitBreakerHaveRetry Retry(int retry)
        {
            _retry = retry;
            return this;
        }

        public ICircuitBreakerHaveTimeout Timeout(TimeSpan timeSpan)
        {
            _timeout = timeSpan;
            return this;
        }

        private ICircuitRequest BuildRequest()
        {
            var request = new CircuitRequest
            {
                CircuitKey = _circuitKey,
                Timeout = _timeout,
                RequestId = _requestId,
                Retry = _retry
            };

            if (!string.IsNullOrWhiteSpace(_appName))
            {
                request.Context.SetAppName(_appName);
            }

            if (!string.IsNullOrWhiteSpace(_serviceName))
            {
                request.Context.SetServiceName(_serviceName);
            }

            return request;
        }
    }
}
