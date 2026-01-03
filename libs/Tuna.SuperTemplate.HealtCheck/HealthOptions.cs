namespace Tuna.SuperTemplate.HealtCheck;

public class HealthOptions
{
    public bool Enabled { get; set; } = true;

    public string HealthEndpoint { get; set; } = "/health";
    public string AlivenessEndpoint { get; set; } = "/alive";
    public string UIPath { get; set; } = "/health-ui";

    public string LivenessTag { get; set; } = "live";

    public bool UseInMemoryStorageForUI { get; set; } = true;
    public int EvaluationInterval { get; set; } = 10;
    public int MaxHistoryEntries { get; set; } = 60;
}
