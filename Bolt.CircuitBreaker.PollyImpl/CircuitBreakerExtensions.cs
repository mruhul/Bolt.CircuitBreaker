using Bolt.CircuitBreaker.Abstracts;
using System;

namespace Bolt.CircuitBreaker.PollyImpl
{
    internal static class CircuitBreakerExtensions
    {
        public static Tuple<string,string> SetupDefaultContext(this ICircuitRequest request)
        {
            var appName = request.Context.GetAppName();
            if (string.IsNullOrWhiteSpace(appName))
            {
                appName = AppDomain.CurrentDomain.FriendlyName;
                request.Context.SetAppName(appName);
            }
            var serviceName = request.Context.GetServiceName();
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                serviceName = request.CircuitKey;
                request.Context.SetServiceName(serviceName);
            }

            return new Tuple<string, string>(appName, serviceName);
        }
    }
}
