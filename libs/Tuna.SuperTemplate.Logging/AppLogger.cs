using Microsoft.Extensions.Logging;
using Tuna.SuperTemplate.Logging.Interface;
namespace Tuna.SuperTemplate.Logging;

public sealed class AppLogger<T> : IAppLogger<T>
{
    private readonly ILogger<T> _logger;

    public AppLogger(ILogger<T> logger) => _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public void LogTrace(string message, params object[] args) => _logger.LogTrace(message, args);
    public void LogDebug(string message, params object[] args) => _logger.LogDebug(message, args);
    public void LogInformation(string message, params object[] args) => _logger.LogInformation(message, args);
    public void LogWarning(string message, params object[] args) => _logger.LogWarning(message, args);

    public void LogError(Exception? ex, string message, params object[] args)
    {
        if (ex is null) _logger.LogError(message, args);
        else _logger.LogError(ex, message, args);
    }

    public void LogCritical(Exception? ex, string message, params object[] args)
    {
        if (ex is null) _logger.LogCritical(message, args);
        else _logger.LogCritical(ex, message, args);
    }
}
