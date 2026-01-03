namespace Tuna.SuperTemplate.Resilience.Configuration;

public class ResilienceOptions
{
    public int RetryCount { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 2000;

    public int CircuitBreakerFailures { get; set; } = 5;
    public int CircuitBreakerDurationSec { get; set; } = 30;

    public int TimeoutSec { get; set; } = 10;

    public int MaxParallelization { get; set; } = 10;
    public int MaxQueuingActions { get; set; } = 20;
}

