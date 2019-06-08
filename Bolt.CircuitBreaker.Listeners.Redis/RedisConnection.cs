using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Bolt.CircuitBreaker.Listeners.Redis
{
    public class RedisConnection : IRedisConnection
    {
        private static readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
        private static IConnectionMultiplexer _connection;
        private static DateTime? _lastFailure;
        private static int _breakInSeconds = 5;
        private const int _breakIncrement = 30;

        private readonly RedisConnectionSettings _options;
        private readonly ILogger<RedisConnection> _logger;

        public RedisConnection(IOptions<RedisConnectionSettings> options, ILogger<RedisConnection> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task<IConnectionMultiplexer> GetOrCreate()
        {
            if (_connection != null) return _connection;

            await _semaphoreSlim.WaitAsync();

            try
            {
                if (_connection != null) return _connection;

                if (_lastFailure.HasValue && DateTime.UtcNow.Subtract(_lastFailure.Value).TotalSeconds < _breakInSeconds)
                {
                    _logger.LogTrace($"Skip connection as it was failed so will try after {_breakInSeconds}");

                    return _connection;
                }

                _connection = await ConnectionMultiplexer.ConnectAsync(_options.ConnectionString);

                _lastFailure = null;
            }
            catch(Exception e)
            {
                _lastFailure = DateTime.UtcNow;

                if(_breakInSeconds < int.MaxValue - _breakIncrement)
                {
                    _breakInSeconds = _breakInSeconds + _breakIncrement;
                }

                _logger.LogError(e, e.Message);
            }
            finally
            {
                _semaphoreSlim.Release();
            }

            return _connection;
        }

        public async Task<IDatabase> Database(int? db = null)
        {
            var con = await GetOrCreate();

            return con?.GetDatabase(db ?? _options.Db);
        }
    }

    public interface IRedisConnection
    {
        Task<IConnectionMultiplexer> GetOrCreate();
        Task<IDatabase> Database(int? db = null);
    }

    public class RedisConnectionSettings
    {
        public string ConnectionString { get; set; }
        public int Db { get; set; }
    }
}
