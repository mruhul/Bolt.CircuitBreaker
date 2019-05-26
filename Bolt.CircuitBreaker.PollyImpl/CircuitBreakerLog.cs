using Microsoft.Extensions.Logging;
using System;

namespace Bolt.CircuitBreaker.PollyImpl
{
    public static class CircuitBreakerLog
    {
        private static ILogger _logger;

        public static void Init(ILoggerFactory factory)
        {
            _logger = factory.CreateLogger("Bolt.PollyCircuitBreaker");
        }

        internal static bool IsTraceEnabled => _logger?.IsEnabled(LogLevel.Trace) ?? false;

        internal static void LogError(Exception e, string message)
        {
            _logger?.LogError(e, message);
        }

        internal static void LogTrace(string message)
        {
            _logger?.LogTrace(message);
        }

        internal static void LogWarning(string message)
        {
            _logger?.LogWarning(message);
        }
    }
}
