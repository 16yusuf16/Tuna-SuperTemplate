using Polly;
using Polly.Bulkhead;
using Tuna.SuperTemplate.Logging.Interface;
using Tuna.SuperTemplate.Resilience.Configuration;

namespace Tuna.SuperTemplate.Resilience.Policies;

public class BulkheadPolicyProvider(IAppLogger<BulkheadPolicyProvider> _logger)
{

    public AsyncBulkheadPolicy GetBulkheadPolicy(ResilienceOptions options)
    {
        return Policy.BulkheadAsync(options.MaxParallelization, options.MaxQueuingActions,
            onBulkheadRejectedAsync: context =>
            {
                _logger.LogWarning("Bulkhead rejected execution.");
                return Task.CompletedTask;
            });
    }
}
