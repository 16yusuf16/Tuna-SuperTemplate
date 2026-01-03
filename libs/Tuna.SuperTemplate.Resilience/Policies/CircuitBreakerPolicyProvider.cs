using Polly;
using Polly.CircuitBreaker;
using Tuna.SuperTemplate.Logging.Interface;
using Tuna.SuperTemplate.Resilience.Configuration;

namespace Tuna.SuperTemplate.Resilience.Policies;

public class CircuitBreakerPolicyProvider(IAppLogger<CircuitBreakerPolicyProvider> _logger)
{
    public AsyncCircuitBreakerPolicy GetCircuitBreakerPolicy(ResilienceOptions options)
    {
        return Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(options.CircuitBreakerFailures,
                                 TimeSpan.FromSeconds(options.CircuitBreakerDurationSec),
                                 onBreak: (ex, ts) => _logger.LogError(ex, "Circuit broken!"),
                                 onReset: () => _logger.LogInformation("Circuit reset"),
                                 onHalfOpen: () => _logger.LogInformation("Circuit half-open"));
    }
}
