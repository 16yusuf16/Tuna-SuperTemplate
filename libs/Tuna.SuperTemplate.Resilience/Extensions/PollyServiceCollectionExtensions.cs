using Microsoft.Extensions.DependencyInjection;
using Tuna.SuperTemplate.Logging;
using Tuna.SuperTemplate.Logging.Interface;
using Tuna.SuperTemplate.Resilience.Policies;

namespace Tuna.SuperTemplate.Resilience.Extensions;

public static class PollyServiceCollectionExtensions
{
    public static IServiceCollection AddResiliencePolicies(this IServiceCollection services)
    {
        services.AddScoped<RetryPolicyProvider>();
        services.AddScoped<CircuitBreakerPolicyProvider>();
        services.AddScoped<TimeoutPolicyProvider>();
        services.AddScoped<BulkheadPolicyProvider>();

        services.AddScoped<IResilienceService, ResilienceService>();

        return services;
    }
}
