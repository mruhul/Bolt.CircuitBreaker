using Bolt.CircuitBreaker.Abstracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bolt.CircuitBreaker.Listeners.Redis
{
    public static class IocSetup
    {
        public static IServiceCollection AddRedisListenerForCircuitBreaker(this IServiceCollection source, IConfiguration configuration, RedisListenerOptions options = null)
        {
            options = options ?? new RedisListenerOptions();

            source.Configure<RedisConnectionSettings>(configuration.GetSection(options.RedisSettingsPath));
            source.TryAddSingleton<IRedisConnection, Redis.RedisConnection>();
            source.TryAddEnumerable(ServiceDescriptor.Singleton<ICircuitStatusListener, Redis.CircuitStatusListener>());

            return source;
        }
    }

    public class RedisListenerOptions
    {
        public string RedisSettingsPath = "Bolt:CircuitBreaker:Redis";
    }
}
