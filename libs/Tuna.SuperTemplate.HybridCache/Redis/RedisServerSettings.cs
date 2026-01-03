namespace Tuna.SuperTemplate.HybridCache.Redis;

public class RedisServerSettings
{
    public string ConnectionString { get; set; }
    public bool AbortOnConnectFail { get; set; }
    public int AsyncTimeoutMilliSecond { get; set; }
    public int ConnectTimeoutMilliSecond { get; set; }
    public string Password { get; set; }
    public bool AllowAdmin { get; set; }
    public int DefaultDatabase { get; set; }
}
