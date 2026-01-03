
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using OneOf;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Tuna.SuperTemplate.HybridCache.Hybrid;

public interface IMemoryCacheManager :ICacheManager
{
    Task<bool> RemoveAsync(List<string> keys, CancellationToken cancellationToken);
}
internal class Entry<T>
{
    public Entry() { }
    public T? Value { get; set; } = default;
    public Entry(T value) { Value = value; }
}
public class MemoryCacheManager : IMemoryCacheManager
{
    public string InstanceId { get; }
    readonly IMemoryCache _cache;
    readonly CacheSettings _settings;

    static CancellationTokenSource _cancellationTokenSource;
    static CancellationChangeToken _cancellationChangeToken;
    static readonly ConcurrentDictionary<string, DateTime> _keys;
    static readonly ConcurrentDictionary<string, bool> _locks;
    private readonly ILogger<MemoryCacheManager> _logger;



    public event EventHandler<string> CacheUpdated;
    public event EventHandler<string> CacheRemoved;
    public event EventHandler CacheCleared;

    static MemoryCacheManager()
    {
        _keys = new ConcurrentDictionary<string, DateTime>();
        _locks = new ConcurrentDictionary<string, bool>();
        _cancellationTokenSource = new CancellationTokenSource();
        _cancellationChangeToken = new CancellationChangeToken(_cancellationTokenSource.Token);
    }
    public MemoryCacheManager(IMemoryCache cache, CacheSettings settings, ILogger<MemoryCacheManager> logger)
    {
        InstanceId = Guid.NewGuid().ToString();
        _settings = settings;
        _logger = logger;
        _cache = cache;

        CacheUpdated += OnCacheUpdated;
        CacheRemoved += OnCacheRemoved;
        CacheCleared += OnCacheCleared;
    }

    private void OnCacheCleared(object? sender, EventArgs e)
    {
        _logger.LogDebug("{SenderTypeName} cleared.", sender?.GetType()?.Name);
    }

    private void OnCacheRemoved(object? sender, string key)
    {
       _logger.LogDebug("{SenderTypeName} removed: {Key}.", sender?.GetType()?.Name, key);
    }

    private void OnCacheUpdated(object? sender, string key)
    {
        _logger.LogDebug("{SenderTypeName} updated: {Key}.", sender?.GetType()?.Name, key);
    }
    private static IEnumerable<string> Keys
    {
        get
        {
            foreach (var item in _keys.Where(x=>x.Value> DateTime.Now))
            {
                yield return item.Key;
            }
        }
    }

    private void AddKey(string key , TimeSpan? expiry = null)
    {
        expiry ??= _settings.MemExpiryTime;
        DateTime expiryDate = DateTime.Now.AddMilliseconds(expiry.Value.TotalMilliseconds - 300);
        ClearExpiredKeys();
        if (!_keys.TryAdd(key, expiryDate) && _keys.TryGetValue(key, out var currentVal))
            _keys.TryUpdate(key, expiryDate, currentVal);
    }
    private string RemoveKey(string key)
    {
        _keys.TryRemove(key, out _);
        return key;
    }

    private void ClearExpiredKeys()
    {
        var expiredKeys = _keys.Where(x=>x.Value <DateTime.Now).Select(x=>x.Key);
        foreach (var item in expiredKeys)
        {
            _keys.TryRemove(item, out _);
        }
    }

    private async Task<OneOf<bool, Exception>> CacheWaiterAsync(string key)
    {
        var delay = 80;
        int total = 0;
        while (!_locks.TryAdd(key, true))
        {
            await Task.Delay(delay);
            total += delay;
            if (total >= 5000)
            {
                _logger.LogWarning("CacheWaiterAsync timeout for key {Key}.", key);
                return new TimeoutException($"CacheWaiterAsync timeout for key {key}.");
            }
        }
        return true;
    }

    private OneOf<bool, Exception> CacheWaiter(string key)
    {
        var delay = 80;
        int total = 0;
        while (_locks.ContainsKey(key))
        {
            Thread.Sleep(delay);
            total += delay;
            if (total >= 5000)
            {
                _logger.LogWarning("CacheWaiter timeout for key {Key}.", key);
                return new TimeoutException($"CacheWaiter timeout for key {key}.");
            }

        }
        return true;
    }
    #region Methods

    private MemoryCacheEntryOptions GetMemoryCacheEntryOptions(TimeSpan? expiry = null)
    {
        var options = new MemoryCacheEntryOptions()
        .AddExpirationToken(MemoryCacheManager._cancellationChangeToken);

        options.AbsoluteExpirationRelativeToNow = expiry ?? _settings.MemExpiryTime;

        return options;
    }

    public async Task<T?> GetAsync<T>(string key, Func<Task<T?>>? provider = null, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key)) return default;

        try
        {
            var entry = _cache.Get<Entry<T>>(key);

            if (entry != null)
                return entry.Value;

            if (provider is null)
                return default;

            if (_locks.TryAdd(key, true))
            {
                T? result;
                try
                {
                    result = await provider();
                    await SetAsync(key, result, expiry, ct);
                }
                finally
                {
                    _locks.TryRemove(key, out _);
                }

                return result;
            }
            else
            {
                await CacheWaiterAsync(key);

                return await GetAsync(key, provider, expiry, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache GetAsync error for {Key}", key);

            return provider is null ? default : await provider();
        }
    }

    public T? Get<T>(string key, Func<T?>? provider = null, TimeSpan? expiry = null)
    {
        if (string.IsNullOrWhiteSpace(key)) return default;

        try
        {
            var entry = _cache.Get<Entry<T>>(key);

            if (entry != null)
                return entry.Value;

            if (provider is null)
                return default;

            if (_locks.TryAdd(key, true))
            {
                T? result;
                try
                {
                    result = provider();
                    Set(key, result, expiry);
                }
                finally
                {
                    _locks.TryRemove(key, out _);
                }

                return result;
            }
            else
            {
                var sw = new Stopwatch();
                sw.Start();

                CacheWaiter(key);

                _logger.LogWarning("Memory cache waiting for {Key} ms => {ElapsedMilliseconds}", key, sw.ElapsedMilliseconds);

                return Get(key, provider, expiry);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache Get error for {Key}", key);

            return provider is null ? default : provider();
        }
    }

    public async Task<T?> GetOnlyAsync<T>(string key, Func<Task<T?>>? provider = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key)) return default;

        try
        {
            var entry = _cache.Get<Entry<T>>(key);

            if (entry != null)
                return entry.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache GetOnlyAsync error for {Key}", key);
        }

        return provider is null ? default : await provider();
    }

    public T? GetOnly<T>(string key, Func<T?>? provider = null)
    {
        if (string.IsNullOrWhiteSpace(key)) return default;

        try
        {
            var entry = _cache.Get<Entry<T>>(key);

            if (entry != null)
                return entry.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache GetOnly error for {Key}", key);
        }

        return provider is null ? default : provider();
    }

    public async Task<T?> GetDefaultAsync<T>(string key, Func<Task<T?>>? provider = null, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key)) return default;

        if (provider is null)
            return await GetAsync(key, () => Task.FromResult(default(T)), expiry, ct);
        else
            return await GetAsync(key, provider, expiry, ct);
    }

    public T? GetDefault<T>(string key, Func<T?>? provider = null, TimeSpan? expiry = null)
    {
        if (string.IsNullOrWhiteSpace(key)) return default;

        if (provider is null)
            return Get(key, () => default(T), expiry);
        else
            return Get(key, provider, expiry);
    }

    public Task<bool> SetAsync<T>(string key, T? data, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        return Task.FromResult(Set(key, data, expiry));
    }

    public bool Set<T>(string key, T? data, TimeSpan? expiry = null)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;

        try
        {
            _cache.Set(key, new Entry<T>(data), GetMemoryCacheEntryOptions(expiry));

            AddKey(key, expiry);

            CacheUpdated?.Invoke(this, key);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache Set error for {Key} - Expiry: {Expiry}", key, expiry);

            return false;
        }
    }

    public Task<bool> SetBulkAsync<T>(IDictionary<string, T?> items, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        throw new NotSupportedException("Bulk set operations are not supported in the memory cache implementation by design. Use individual Set operations instead.");
    }

    public Task<Dictionary<string, T>> GetKeysAndValuesByPrefixAsync<T>(string prefix, CancellationToken ct = default)
    {
        throw new NotSupportedException("GetKeysAndValuesByPrefixAsync is not supported in the memory cache implementation by design. Use individual Get operations instead.");
    }

    public bool Exists(string key)
    {
        try
        {
            return Keys.Contains(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache Exists error for {Key}", key);

            return false;
        }
    }

    public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        return Task.FromResult(Exists(key));
    }

    public Task<bool> RemoveAsync(string key, CancellationToken ct = default)
    {
        return Task.FromResult(Remove(key));
    }

    public Task<bool> RemoveAsync(List<string> keys, CancellationToken ct = default)
    {
        if (keys == null || !keys.Any()) return Task.FromResult(false);

        foreach (var key in keys)
            Remove(key);

        return Task.FromResult(true);
    }

    public bool Remove(string key)
    {
        try
        {
            _cache.Remove(RemoveKey(key));

            CacheRemoved?.Invoke(this, key);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache Remove error for {Key}", key);

            return false;
        }
    }

    public bool RemoveByPrefix(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix)) return false;

        var keys = GetKeys(prefix);

        if (!keys.Any()) return false;

        foreach (var key in keys)
            Remove(key);

        return true;
    }

    public Task<bool> RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        return Task.FromResult(RemoveByPrefix(prefix));
    }

    public Task ClearAsync(CancellationToken ct = default)
    {
        Clear();

        return Task.CompletedTask;
    }

    public void Clear()
    {
        try
        {
            CancellationTokenSource ts = new();
            MemoryCacheManager._cancellationChangeToken = new CancellationChangeToken(ts.Token);

            _keys.Clear();

            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            MemoryCacheManager._cancellationTokenSource = ts;

            CacheCleared?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache Clear error");
        }
    }

    public Task<List<string>> GetKeysAsync(string? prefix = default, CancellationToken ct = default)
    {
        return Task.FromResult(GetKeys(prefix));
    }

    public List<string> GetKeys(string? prefix = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(prefix) || prefix.Trim() == "*")
                return Keys.ToList();

            if (prefix.EndsWith('*'))
                prefix = prefix.Remove(prefix.Length - 1, 1);

            return Keys.Where(key => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache GetKeys error for {Prefix}", prefix);

            return new List<string>();
        }
    }

    #endregion
}
