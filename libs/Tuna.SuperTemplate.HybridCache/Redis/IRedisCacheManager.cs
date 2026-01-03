using StackExchange.Redis;

namespace Tuna.SuperTemplate.HybridCache.Redis
{
    public interface IRedisCacheManager
    {
        Task ClearAsync();
        Task<T> GetAsync<T>(string key);
        Task<List<T>> GetManyAsync<T>(string key);
        Task<bool> IsSetAsync(string key);
        Task RemoveAsync(string key);
        Task SetAsync<T>(string key, object data, TimeSpan? expiry = null);
        Task SetAsync<T>(string key, object data);
        Task<List<string>> GetAllKeysAsync();
        Task<T> GetAllKeysByPatternAsync<T>(string pattern);
        Task<bool> BulkSetAsync(IEnumerable<KeyValuePair<RedisKey, RedisValue>> items);

    }
}
