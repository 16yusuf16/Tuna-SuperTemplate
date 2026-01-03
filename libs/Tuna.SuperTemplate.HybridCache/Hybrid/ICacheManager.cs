namespace Tuna.SuperTemplate.HybridCache.Hybrid;

public interface ICacheManager
{
    event EventHandler<string> CacheUpdated;
    event EventHandler<string> CacheRemoved;
    event EventHandler CacheCleared;
    string InstanceId { get; }

    Task<T?> GetAsync<T>(string key, Func<Task<T?>>? provider = null, TimeSpan? expiry = null, CancellationToken ct = default);

    T? Get<T>(string key, Func<T?>? provider = null, TimeSpan? expiry = null);

    Task<T?> GetOnlyAsync<T>(string key, Func<Task<T?>>? provider = null, CancellationToken ct = default);
    T? GetOnly<T>(string key, Func<T?>? provider = null);

    Task<T?> GetDefaultAsync<T>(string key, Func<Task<T?>>? provider = null, TimeSpan? expiry = null, CancellationToken ct = default);

    T? GetDefault<T>(string key, Func<T?>? provider = null, TimeSpan? expiry = null);

    Task<bool> SetAsync<T>(string key, T? data, TimeSpan? expiry = null, CancellationToken ct = default);

    bool Set<T>(string key, T? data, TimeSpan? expiry = null);
    Task<bool> SetBulkAsync<T>(IDictionary<string, T?> items, TimeSpan? expiry = null, CancellationToken ct = default);

    Task<Dictionary<string, T>> GetKeysAndValuesByPrefixAsync<T>(string prefix, CancellationToken ct = default);

    Task<bool> ExistsAsync(string key, CancellationToken ct = default);

    bool Exists(string key);

    Task<bool> RemoveAsync(string key, CancellationToken ct = default);

    bool Remove(string key);

    Task<bool> RemoveByPrefixAsync(string prefix, CancellationToken ct = default);

    bool RemoveByPrefix(string prefix);

    Task ClearAsync(CancellationToken ct = default);

    void Clear();

    Task<List<string>> GetKeysAsync(string? prefix = default, CancellationToken ct = default);

    List<string> GetKeys(string? prefix = default);
}
