namespace Tuna.SuperTemplate.Logging.Interface;

public interface IAppLogger<T>
{
    void LogTrace(string message, params object[] args);
    void LogDebug(string message, params object[] args);
    void LogInformation(string message, params object[] args);
    void LogWarning(string message, params object[] args);
    void LogError(System.Exception? ex, string message, params object[] args);
    void LogCritical(System.Exception? ex, string message, params object[] args);
}
