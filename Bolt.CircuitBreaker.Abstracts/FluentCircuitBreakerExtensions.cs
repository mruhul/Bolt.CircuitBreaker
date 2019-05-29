using Bolt.CircuitBreaker.Abstracts.Fluent;
using System;
using System.Threading.Tasks;

namespace Bolt.CircuitBreaker.Abstracts
{
    public static class FluentCircuitBreakerExtensions
    {
        public static ICircuitBreakerHaveCircuitKey New(this ICircuitBreaker circuitBreaker, string circuitKey)
        {
            return new FluentCircuitBreaker(circuitBreaker, circuitKey, null, null);
        }

        public static ICircuitBreakerHaveCircuitKey New(this ICircuitBreaker circuitBreaker, string circuitKey, string appName, string serviceName)
        {
            return new FluentCircuitBreaker(circuitBreaker, circuitKey, appName, serviceName);
        }
    }
}
