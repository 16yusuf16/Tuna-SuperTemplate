using Polly;
using Polly.Timeout;
using Tuna.SuperTemplate.Logging.Interface;
using Tuna.SuperTemplate.Resilience.Configuration;

namespace Tuna.SuperTemplate.Resilience.Policies;

public  class TimeoutPolicyProvider(IAppLogger<TimeoutPolicyProvider> _logger)
{
       public AsyncTimeoutPolicy GetTimeoutPolicy(ResilienceOptions options)
    {
        return Policy.TimeoutAsync(
            TimeSpan.FromSeconds(options.TimeoutSec),
            TimeoutStrategy.Pessimistic,
            onTimeoutAsync: (context, timespan, task) =>
            {
                _logger.LogWarning("Operation timed out after {TimeoutSec} seconds.", timespan.TotalSeconds);
                return Task.CompletedTask;
            });
    }
}
