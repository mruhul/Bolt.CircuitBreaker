using System;
using System.Threading.Tasks;
using Bolt.CircuitBreaker.Abstracts;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Bolt.CircuitBreaker.Listeners.Redis
{
    public class CircuitStatusListener : ICircuitStatusListener
    {
        private readonly IRedisConnection _redis;
        private readonly ILogger<CircuitStatusListener> _logger;

        public CircuitStatusListener(IRedisConnection redis, ILogger<CircuitStatusListener> logger)
        {
            _redis = redis;
            _logger = logger;
        }

        private const string _servicesKey = "apm:services";
        private const string _formatHr = "yy:MM:dd:HH";
        private const string _formatMn = "yy:MM:dd:HH:mm";
        public async Task Notify(ICircuitStatusData statusData)
        {
            var appName = statusData.Context?.GetAppName();
            var serviceName = statusData.Context?.GetServiceName();

            if(string.IsNullOrWhiteSpace(appName))
            {
                appName = AppDomain.CurrentDomain.FriendlyName;
            }

            if(string.IsNullOrWhiteSpace(serviceName))
            {
                serviceName = statusData.CircuitKey;
            }

            if (string.IsNullOrWhiteSpace(appName) || string.IsNullOrWhiteSpace(serviceName)) return;

            var db = await _redis.Database();

            if (db == null) return;

            var datetime = DateTime.UtcNow;

            var hrHashKey = $"apm:{appName}:{datetime.ToString(_formatHr)}";
            var mnHashKey = $"apm:{appName}:{datetime.ToString(_formatMn)}";

            db.HashIncrement(hrHashKey, $"{serviceName}:{statusData.Status}", 1, CommandFlags.FireAndForget);
            db.HashIncrement(mnHashKey, $"{serviceName}:{statusData.Status}", 1, CommandFlags.FireAndForget);

            if (statusData.Status == CircuitStatus.Succeed)
            {
                var executionTime = statusData.ExecutionTime.TotalMilliseconds;
                db.HashIncrement(hrHashKey, $"{serviceName}:time", executionTime, CommandFlags.FireAndForget);
                db.HashIncrement(mnHashKey, $"{serviceName}:time", executionTime, CommandFlags.FireAndForget);
            }

            var activityTime = datetime.ToString("o");
            db.HashSet(_servicesKey, appName, activityTime, When.Always, CommandFlags.FireAndForget);
            db.HashSet(_servicesKey, serviceName, activityTime, When.Always, CommandFlags.FireAndForget);
            db.HashSet($"apm:{serviceName}:lastused", appName, activityTime, When.Always, CommandFlags.FireAndForget);

            db.KeyExpire(hrHashKey, datetime.AddDays(7), CommandFlags.FireAndForget);
            db.KeyExpire(mnHashKey, datetime.AddMinutes(30), CommandFlags.FireAndForget);

            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace($"RequestId:{statusData.RequestId}|AppName:{appName}|ServiceName:{serviceName}|CircuitKey:{statusData.CircuitKey}|Status:{statusData.Status}|ExecutionTime:{statusData.ExecutionTime.TotalMilliseconds}ms");
            }
        }
    }
}
