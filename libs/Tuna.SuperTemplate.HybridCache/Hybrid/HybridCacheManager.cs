using Microsoft.Extensions.Logging;

namespace Tuna.SuperTemplate.HybridCache.Hybrid;

public sealed class HybridCacheManager : ICacheManager
{
    #region Fields

    public string InstanceId { get; }
    readonly IMemoryCacheManager _primary;
    readonly IDistributedCacheManager _secondary;
    private readonly ILogger<HybridCacheManager> _logger;

    #endregion

    #region Events

    public event EventHandler<string> CacheUpdated;
    public event EventHandler<string> CacheRemoved;
    public event EventHandler CacheCleared;

    #endregion

    #region Constructor

    public HybridCacheManager(IMemoryCacheManager primaryCache, IDistributedCacheManager secondaryCache, CacheSettings settings, ILogger<HybridCacheManager> logger)
    {
        InstanceId = Guid.NewGuid().ToString();

        _logger = logger;
        _primary = primaryCache;
        _secondary = secondaryCache;

        CacheUpdated += OnCacheUpdated;
        CacheRemoved += OnCacheRemoved;
        CacheCleared += OnCacheCleared;
    }

    #endregion

    #region Methods

    public async Task<T?> GetAsync<T>(string key, Func<Task<T?>>? provider = null, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key)) return default;

        if (provider is null)
            return await GetOnlyAsync<T>(key);

        return await _primary.GetAsync(key, async () => await _secondary.GetAsync(key, provider, expiry, ct), null, ct);
    }

    public T? Get<T>(string key, Func<T?>? provider = null, TimeSpan? expiry = null)
    {
        if (string.IsNullOrWhiteSpace(key)) return default;

        if (provider is null)
            return GetOnly<T>(key);

        return _primary.Get(key, () => _secondary.Get(key, provider, expiry));
    }

    public async Task<T?> GetOnlyAsync<T>(string key, Func<Task<T?>>? provider = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key)) return default;

        return await _primary.GetOnlyAsync(key, async () => await _secondary.GetOnlyAsync(key, provider, ct), ct);
    }

    public T? GetOnly<T>(string key, Func<T?>? provider = null)
    {
        if (string.IsNullOrWhiteSpace(key)) return default;

        return _primary.GetOnly(key, () => _secondary.GetOnly(key, provider));
    }

    public async Task<T?> GetDefaultAsync<T>(string key, Func<Task<T?>>? provider = null, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key)) return default;

        return await _primary.GetAsync(key, async () => await _secondary.GetOnlyAsync(key, provider, ct), expiry, ct);
    }

    public T? GetDefault<T>(string key, Func<T?>? provider = null, TimeSpan? expiry = null)
    {
        if (string.IsNullOrWhiteSpace(key)) return default;

        return _primary.Get(key, () => _secondary.GetOnly(key, provider), expiry);
    }

    public async Task<bool> SetAsync<T>(string key, T? data, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;

        await _primary.SetAsync(key, data, expiry, ct);

        return await _secondary.SetAsync(key, data, expiry, ct);
    }

    public bool Set<T>(string key, T? data, TimeSpan? expiry = null)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;

        _primary.Set(key, data, expiry);

        return _secondary.Set(key, data, expiry);
    }

    public async Task<bool> SetBulkAsync<T>(IDictionary<string, T?> items, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        if (items == null || !items.Any()) return false;

        var validItems = items.Where(kv => !string.IsNullOrWhiteSpace(kv.Key))
       .ToDictionary(kv => kv.Key, kv => kv.Value);

        if (!validItems.Any()) return false;

        await _primary.RemoveAsync(validItems.Keys.ToList(), ct);

        return await _secondary.SetBulkAsync(validItems, expiry, ct);
    }

    public async Task<Dictionary<string, T>> GetKeysAndValuesByPrefixAsync<T>(string prefix, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(prefix))
        {
            return new Dictionary<string, T>();
        }

        return await _secondary.GetKeysAndValuesByPrefixAsync<T>(prefix);
    }

    public bool Exists(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;

        return _primary.Exists(key) || _secondary.Exists(key);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;

        return await _primary.ExistsAsync(key, ct) || await _secondary.ExistsAsync(key, ct);
    }

    public async Task<bool> RemoveAsync(string key, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;

        await _primary.RemoveAsync(key, ct);

        return await _secondary.RemoveAsync(key, ct);
    }

    public bool Remove(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;

        _primary.Remove(key);

        return _secondary.Remove(key);
    }

    public bool RemoveByPrefix(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix)) return false;

        _primary.RemoveByPrefix(prefix);

        return _secondary.RemoveByPrefix(prefix);
    }

    public async Task<bool> RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(prefix)) return false;

        await _primary.RemoveByPrefixAsync(prefix, ct);

        return await _secondary.RemoveByPrefixAsync(prefix, ct);
    }

    public async Task<List<string>> GetKeysAsync(string? prefix = default, CancellationToken ct = default)
    {
        return await _secondary.GetKeysAsync(prefix, ct);
    }

    public List<string> GetKeys(string? prefix = default)
    {
        return _secondary.GetKeys(prefix);
    }

    public async Task ClearAsync(CancellationToken ct = default)
    {
        await _primary.ClearAsync(ct);
        await _secondary.ClearAsync(ct);
    }

    public void Clear()
    {
        _primary.Clear();
        _secondary.Clear();
    }

    #endregion

    #region Events

    private void OnCacheUpdated(object? sender, string key)
    {
        _logger.LogDebug("{SenderTypeName} updated: {Key}.", sender?.GetType()?.Name, key);
    }

    private void OnCacheRemoved(object? sender, string key)
    {
        _logger.LogDebug("{SenderTypeName} removed: {Key}.", sender?.GetType()?.Name, key);
    }

    private void OnCacheCleared(object? sender, EventArgs e)
    {
        _logger.LogDebug("{SenderTypeName} cleared.", sender?.GetType()?.Name);
    }

    #endregion
}
