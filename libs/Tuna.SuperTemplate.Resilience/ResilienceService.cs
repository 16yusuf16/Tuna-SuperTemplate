using Microsoft.Extensions.Options;
using Polly.Wrap;
using Tuna.SuperTemplate.Resilience.Configuration;
using Tuna.SuperTemplate.Resilience.Extensions;
using Tuna.SuperTemplate.Resilience.Policies;

namespace Tuna.SuperTemplate.Resilience;

public interface IResilienceService
{
    AsyncPolicyWrap PolicyWrap { get; }
}

public class ResilienceService : IResilienceService
{
    public AsyncPolicyWrap PolicyWrap { get; }

    public ResilienceService(
        IOptions<ResilienceOptions> options,
        RetryPolicyProvider retryProvider,
        CircuitBreakerPolicyProvider circuitProvider,
        TimeoutPolicyProvider timeoutProvider,
        BulkheadPolicyProvider bulkheadProvider)
    {
        PolicyWrap = PollyExtensions.CreateDefaultPolicyWrap(
            options.Value, retryProvider, circuitProvider, timeoutProvider, bulkheadProvider);
    }
}
