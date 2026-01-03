using Polly;
using Tuna.SuperTemplate.Logging.Interface;
using Tuna.SuperTemplate.Resilience.Configuration;

public class RetryPolicyProvider(IAppLogger<RetryPolicyProvider> _logger)
{

    public IAsyncPolicy GetRetryPolicy(ResilienceOptions options)
    {
        return Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(options.RetryCount,
                attempt => TimeSpan.FromMilliseconds(options.RetryDelayMs),
                (exception, timespan, retryCount, context) =>
                {
                    _logger.LogError(exception, "Retry {RetryCount} after {Delay}ms due to: {ExceptionMessage}",
                       retryCount, timespan.TotalMilliseconds, exception.Message);
                });
    }
}
