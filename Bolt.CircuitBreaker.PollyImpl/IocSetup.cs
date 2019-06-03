using Bolt.CircuitBreaker.Abstracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bolt.CircuitBreaker.PollyImpl
{
    public static class IocSetup
    {
        public static IServiceCollection AddPollyCircuitBreaker(this IServiceCollection source, IConfiguration configuration, PollyCircuitBreakerOptions options = null)
        {
            options = options ?? new PollyCircuitBreakerOptions();

            if (options.Enabled)
            {
                source.TryAddTransient<ICircuitBreaker, PollyCircuitBreaker>();
                source.TryAddTransient<IPolicyProvider, PolicyProvider>();

                if (!string.IsNullOrWhiteSpace(options.PolicySettingsConfigPath))
                {
                    source.Configure<PolicySettingsConfig>(configuration.GetSection(options.PolicySettingsConfigPath));
                    source.TryAddEnumerable(ServiceDescriptor.Singleton<IPolicySettingsProvider, ConfigBasedPolicySettingsProvider>());
                }
                source.TryAddEnumerable(ServiceDescriptor.Singleton<IPolicySettingsProvider, ContextBasedPolicySettingsProvider>());
            }
            else
            {
                source.TryAddTransient<ICircuitBreaker, EmptyCircuitBreaker>();
            }

            return source;
        }
    }

    public class PollyCircuitBreakerOptions
    {
        public bool Enabled { get; set; } = true;
        public string PolicySettingsConfigPath = "Bolt:Polly:Settings";
    }
}
