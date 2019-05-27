using System;
using System.Threading.Tasks;
using Bolt.CircuitBreaker.Abstracts;
using StackExchange.Redis;

namespace Bolt.CircuitBreaker.Listeners.Redis
{
    public class CircuitStatusListener : ICircuitStatusListener
    {
        private readonly IRedisConnection _redis;

        public CircuitStatusListener(IRedisConnection redis)
        {
            _redis = redis;
        }

        private const string _servicesKey = "apm:services";
        private const string _formatHr = "yy:MM:dd:HH";
        private const string _formatMn = "yy:MM:dd:HH:mm";
        public async Task Notify(ICircuitStatusData statusData)
        {
            if (string.IsNullOrWhiteSpace(statusData.AppName) || string.IsNullOrWhiteSpace(statusData.ServiceName)) return;

            var db = await _redis.Database();

            if (db == null) return;

            var datetime = DateTime.UtcNow;

            var hrHashKey = $"apm:{statusData.AppName}:{datetime.ToString(_formatHr)}";
            var mnHashKey = $"apm:{statusData.AppName}:{datetime.ToString(_formatMn)}";


            db.HashIncrement(hrHashKey, $"{statusData.ServiceName}:{statusData.Status}", 1, CommandFlags.FireAndForget);
            db.HashIncrement(mnHashKey, $"{statusData.ServiceName}:{statusData.Status}", 1, CommandFlags.FireAndForget);

            if (statusData.Status == CircuitStatus.Succeed)
            {
                db.HashIncrement(hrHashKey, $"{statusData.ServiceName}:succeed", 1, CommandFlags.FireAndForget);
                db.HashIncrement(mnHashKey, $"{statusData.ServiceName}:succeed", 1, CommandFlags.FireAndForget);

                var executionTime = statusData.ExecutionTime.TotalMilliseconds;
                db.HashIncrement(hrHashKey, $"{statusData.ServiceName}:time", executionTime, CommandFlags.FireAndForget);
                db.HashIncrement(mnHashKey, $"{statusData.ServiceName}:time", executionTime, CommandFlags.FireAndForget);
            }
            else if (statusData.Status == CircuitStatus.Failed)
            {
                db.HashIncrement(hrHashKey, $"{statusData.ServiceName}:failed", 1, CommandFlags.FireAndForget);
                db.HashIncrement(mnHashKey, $"{statusData.ServiceName}:failed", 1, CommandFlags.FireAndForget);
            }
            else if (statusData.Status == CircuitStatus.Broken)
            {
                db.HashIncrement(hrHashKey, $"{statusData.ServiceName}:broken", 1, CommandFlags.FireAndForget);
                db.HashIncrement(mnHashKey, $"{statusData.ServiceName}:broken", 1, CommandFlags.FireAndForget);
            }
            else if (statusData.Status == CircuitStatus.Timeout)
            {
                db.HashIncrement(hrHashKey, $"{statusData.ServiceName}:timeout", 1, CommandFlags.FireAndForget);
                db.HashIncrement(mnHashKey, $"{statusData.ServiceName}:timeout", 1, CommandFlags.FireAndForget);
            }

            var activityTime = datetime.ToString("o");
            db.HashSet(_servicesKey, statusData.AppName, activityTime, When.Always, CommandFlags.FireAndForget);
            db.HashSet(_servicesKey, statusData.ServiceName, activityTime, When.Always, CommandFlags.FireAndForget);
            db.HashSet($"apm:{statusData.ServiceName}:lastused", statusData.AppName, activityTime, When.Always, CommandFlags.FireAndForget);

            db.KeyExpire(hrHashKey, datetime.AddDays(7), CommandFlags.FireAndForget);
            db.KeyExpire(mnHashKey, datetime.AddMinutes(30), CommandFlags.FireAndForget);
        }
    }
}
