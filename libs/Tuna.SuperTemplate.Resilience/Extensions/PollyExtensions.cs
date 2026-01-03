using Polly;
using Polly.Wrap;
using Tuna.SuperTemplate.Resilience.Configuration;
using Tuna.SuperTemplate.Resilience.Policies;

namespace Tuna.SuperTemplate.Resilience.Extensions;
public static class PollyExtensions
{
    public static AsyncPolicyWrap CreateDefaultPolicyWrap(
        ResilienceOptions options,
        RetryPolicyProvider retryProvider,
        CircuitBreakerPolicyProvider circuitProvider,
        TimeoutPolicyProvider timeoutProvider,
        BulkheadPolicyProvider bulkheadProvider)
    {
        var retry = retryProvider.GetRetryPolicy(options);
        var circuit = circuitProvider.GetCircuitBreakerPolicy(options);
        var timeout = timeoutProvider.GetTimeoutPolicy(options);
        var bulkhead = bulkheadProvider.GetBulkheadPolicy(options);

        return Policy.WrapAsync(bulkhead, retry, circuit, timeout);
    }

    public static async Task<TRes> ExecuteWithPolicyAsync<TRes>(this AsyncPolicyWrap policy, Func<Task<TRes>> action)
    {
        return await policy.ExecuteAsync(action);
    }

    public static async Task ExecuteWithPolicyAsync(this AsyncPolicyWrap policy, Func<Task> action)
    {
        await policy.ExecuteAsync(action);
    }
}

