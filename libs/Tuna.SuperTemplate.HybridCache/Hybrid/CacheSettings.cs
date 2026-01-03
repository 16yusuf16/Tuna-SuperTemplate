namespace Tuna.SuperTemplate.HybridCache.Hybrid
{
    public enum CacheProvider
    {
        Memory = 0,
        Redis = 1,
        Hybrid = 2
    }
    public class CacheSettings
    {
        public CacheSettings()
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            RemoveSyncChannel = $"cache:remove:{env}";
            ClearSyncChannel = $"cache:clear:{env}";
        }
        public CacheProvider Type { get; set; } = CacheProvider.Memory;
        public TimeSpan MemExpiryTime { get; set; } = TimeSpan.FromSeconds(10);
        public  TimeSpan RedisExpiryTime { get; set; } = TimeSpan.FromMinutes(1);
        public string RemoveSyncChannel { get; set; } 
        public string ClearSyncChannel { get; set; }
    }
}
