using Microsoft.Extensions.Logging;
using OneOf;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Text.Json;
using Tuna.SuperTemplate.HybridCache.Constants;
using Tuna.SuperTemplate.HybridCache.Helper;
using Tuna.SuperTemplate.HybridCache.Redis;

namespace Tuna.SuperTemplate.HybridCache.Hybrid;

public interface IDistributedCacheManager :ICacheManager;
public sealed class RedisCacheManager : IDistributedCacheManager
{
    public string InstanceId { get; }
    readonly CacheSettings _settings;
    readonly RedisServerSettings redisSettings;
    readonly ConnectionMultiplexer _conn;
    readonly IDatabase _database;
    readonly IServer _server;
    readonly ConfigurationOptions _options;
    static readonly ConcurrentDictionary<string, bool> _locks;
    const string _empryValue = "<empty>";
    private readonly ILogger<RedisCacheManager> _logger;


    public event EventHandler<string> CacheUpdated;
    public event EventHandler<string> CacheRemoved;
    public event EventHandler CacheCleared;

    static RedisCacheManager()
    {
        _locks = new ConcurrentDictionary<string, bool>();
    }

    public RedisCacheManager(ConnectionMultiplexer conn ,CacheSettings settings,RedisServerSettings redisServerSetting, ILogger<RedisCacheManager> logger)
    {
        if (redisServerSetting is null) throw new ArgumentNullException(nameof(redisServerSetting));

        var connectionParts = redisServerSetting.ConnectionString?.Split(':');
        if (connectionParts == null || connectionParts.Length != 2)
        {
            throw new ArgumentException("Invalid Redis connection string format. Expected format: host:port", nameof(redisServerSetting.ConnectionString));
        }

        string host = connectionParts[0];
        if (!int.TryParse(connectionParts[1], out int port))
        {
            throw new ArgumentException("Invalid port number in Redis connection string", nameof(redisServerSetting.ConnectionString));
        }
        InstanceId = Guid.NewGuid().ToString();
        _settings = settings;
        _conn = conn;
        _logger = logger;
        var config = new ConfigurationOptions
        {
            AbortOnConnectFail = redisServerSetting.AbortOnConnectFail,
            AsyncTimeout = redisServerSetting.AsyncTimeoutMilliSecond,
            ConnectTimeout = redisServerSetting.ConnectTimeoutMilliSecond,
            Password = redisServerSetting.Password,
            AllowAdmin = redisServerSetting.AllowAdmin,
            DefaultDatabase = redisServerSetting.DefaultDatabase,
            EndPoints = { { host, port } }
        };

        _conn = ConnectionMultiplexer.Connect(config);
        _database = _conn.GetDatabase();
        _server = _conn.GetServer(host, port);

        CacheUpdated += OnCacheUpdated;
        CacheRemoved += OnCacheRemoved;
        CacheCleared += OnCacheCleared;
    }

    public void Clear()
    {
        try
        {
            _server.FlushDatabase(redisSettings.DefaultDatabase);
            CacheCleared?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to clear Redis cache.");
        }

    }

    public async Task ClearAsync(CancellationToken ct = default)
    {
        try
        {
           await  _server.FlushDatabaseAsync(redisSettings.DefaultDatabase);
            CacheCleared?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to clear Redis cache.");
        }
    }

    public async Task<bool> SetAsync<T>(string key, T? data, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;

        try
        {
            var ser = data.Seriliaze(_empryValue);

            var value = ser.Match(
            val => val,
            ex =>
            {
                _logger.LogError(ex, "Serialization error for cache key:{Key}", key);
                return null;
            }
            );

            if (value == null) return false;

            var status = await _database.StringSetAsync(key, value, expiry ?? _settings.RedisExpiryTime);

            CacheUpdated?.Invoke(this, key);

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache SetAsync error for {Key}", key);

            return false;
        }
    }

    public bool Set<T>(string key, T? data, TimeSpan? expiry = null)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;

        try
        {
            var ser = data.Seriliaze(_empryValue);

            var value = ser.Match(
            val => val,
            ex =>
            {
                _logger.LogError(ex, "Serialization error for cache key:{Key}", key);
                return null;
            }
            );

            if (value == null) return false;

            var status = _database.StringSet(key, value, expiry ?? _settings.RedisExpiryTime);

            CacheUpdated?.Invoke(this, key);

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache Set error for {Key}", key);

            return false;
        }
    }

    public async Task<bool> SetBulkAsync<T>(IDictionary<string, T?> items, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        if (items == null || !items.Any()) return false;

        try
        {
            var batch = _database.CreateBatch();
            var tasks = new List<Task<bool>>();
            var validKeys = new List<string>();

            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item.Key)) continue;

                var ser = item.Value.Seriliaze(_empryValue);

                var value = ser.Match(
                val => val,
                ex =>
                {
                    _logger.LogError(ex, "Serialization error for cache key:{Key}", item.Key);
                    return null;
                }
                );

                if (value == null) continue;

                var task = batch.StringSetAsync(item.Key, value, expiry ?? _settings.RedisExpiryTime);
                tasks.Add(task);

                validKeys.Add(item.Key);
            }

            if (validKeys.Count == 0)
            {
                return false;
            }

            batch.Execute();

            await Task.WhenAll(tasks.ToArray());

            var keyListString = string.Join(Cache.Seperator, validKeys);
            CacheUpdated?.Invoke(this, keyListString);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache SetBulkAsync error for {Count} items", items.Count);
            return false;
        }
    }

    public async Task<Dictionary<string, T>> GetKeysAndValuesByPrefixAsync<T>(string prefix, CancellationToken ct = default)
    {
        var keys = _server.Keys(pattern: $"{prefix}:*").ToArray();

        if (keys.Length == 0)
            return [];

        var values = await _database.StringGetAsync(keys);

        var result = new Dictionary<string, T>();

        for (int i = 0; i < keys.Length; i++)
        {
            var key = keys[i];
            var value = values[i];

            if (!value.HasValue)
                continue;

            try
            {
                string json = value.ToString();
                var deserialized = JsonSerializer.Deserialize<T>(json);

                if (deserialized is not null)
                    result[(string)key] = deserialized;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetKeysAndValuesByPrefixAsync #: Error occurred. Details: {Message}", ex.Message);
            }
        }

        return result;
    }

    public bool Exists(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;
        try
        {
            return _database.KeyExists(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check existence of key {Key} in Redis cache.", key);
            return false;
        }

    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(key)) return false;
        try
        {
            return await _database.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check existence of key {Key} in Redis cache.", key);
            return false;
        }
    }

    public T? Get<T>(string key, Func<T?>? provider = null, TimeSpan? expiry = null)
    {
        if (string.IsNullOrEmpty(key)) return default;
        try
        {
            var value = _database.StringGet(key);
            if (value.HasValue)
            {
                if(value.Equals(_empryValue))
                    return default;
                OneOf<T?, Exception> des = value.ToString().DeSeriliaze<T>();
                return  des.Match(
                   val => val,
                   ex =>
                   {
                       _logger.LogError(ex, "Deserilization error for cache key:{Key}", key);
                       return default;
                   }
                );
            }

            if (provider is null) return default;

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
                CacheWaiter(key);
                return Get(key, provider, expiry);
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache get error for Key:{key}", key);
            return provider is null ? default : provider();
        }

    }

    public async Task<T?> GetAsync<T>(string key, Func<Task<T?>>? provider = null, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(key)) return default;
        try
        {
            var value = await _database.StringGetAsync(key);
            if (value.HasValue)
            {
                if (value.Equals(_empryValue))
                    return default;
                OneOf<T?, Exception> des = value.ToString().DeSeriliaze<T>();
                return des.Match(
                   val => val,
                   ex =>
                   {
                       _logger.LogError(ex, "Deserilization error for cache key:{Key}", key);
                       return default;
                   }
                );
            }

            if (provider is null) return default;

            if (_locks.TryAdd(key, true))
            {
                T? result;
                try
                {
                    result = await provider();
                   await SetAsync(key, result, expiry);
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
                return await GetAsync(key, provider, expiry);
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache get error for Key:{key}", key);
            return provider is null ? default :await provider();
        }
    }
    public T? GetOnly<T>(string key, Func<T?>? provider = null)
    {
        if (string.IsNullOrEmpty(key)) return default;
        try
        {
            var value = _database.StringGet(key);
            if (value.HasValue)
            {
                if (value.Equals(_empryValue))
                    return default;
                OneOf<T?, Exception> des = value.ToString().DeSeriliaze<T>();
                return des.Match(
                   val => val,
                   ex =>
                   {
                       _logger.LogError(ex, "Deserilization error for cache key:{Key}", key);
                       return default;
                   }
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache get error for Key:{key}", key);
        }

        return provider is null ? default : provider();
    }

    public async Task<T?> GetOnlyAsync<T>(string key, Func<Task<T?>>? provider = null, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(key)) return default;
        try
        {
            var value = await _database.StringGetAsync(key);
            if (value.HasValue)
            {
                if (value.Equals(_empryValue))
                    return default;
                OneOf<T?, Exception> des = value.ToString().DeSeriliaze<T>();
                return des.Match(
                   val => val,
                   ex =>
                   {
                       _logger.LogError(ex, "Deserilization error for cache key:{Key}", key);
                       return default;
                   }
                );
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache get error for Key:{key}", key);
         
        }
        return provider is null ? default : await provider();
    }


    public bool RemoveByPrefix(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix)) return false;

        try
        {
            var keys = GetKeys(prefix);

            if (!keys.Any()) return false;

            var status = _database.KeyDelete(keys: keys.Select(m => new RedisKey(m)).ToArray()) > 0;

            var joinedKeys = string.Join(Cache.Seperator, keys);
            CacheUpdated?.Invoke(this, joinedKeys);

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache RemoveByPrefix error for {Prefix}", prefix);

            return false;
        }
    }

    public async Task<bool> RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(prefix)) return false;

        try
        {
            var keys = await GetKeysAsync(prefix, ct);

            if (!keys.Any()) return true;

            var status = await _database.KeyDeleteAsync(keys: keys.Select(m => new RedisKey(m)).ToArray()) > 0;

            var joinedKeys = string.Join(Cache.Seperator, keys);
            CacheUpdated?.Invoke(this, joinedKeys);

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache RemoveByPrefixAsync error for {Prefix}", prefix);

            return false;
        }
    }


    public async Task<bool> RemoveAsync(string key, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;

        try
        {
            var status = await _database.KeyDeleteAsync(key);

            CacheRemoved?.Invoke(this, key);

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache RemoveAsync error for {Key}", key);

            return false;
        }
    }

    public bool Remove(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;

        try
        {
            var status = _database.KeyDelete(key);

            CacheRemoved?.Invoke(this, key);

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache Remove error for {Key}", key);

            return false;
        }
    }

    public async Task<List<string>> GetKeysAsync(string? prefix = default, CancellationToken ct = default)
    {
        try
        {
            var keys = _server.KeysAsync(_options.DefaultDatabase ?? -1, prefix);

            var result = new List<string>();
            await foreach (var key in keys)
            {
                result.Add(key.ToString());
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache GetKeysAsync error for {Prefix}", prefix);

            return new List<string>();
        }
    }

    public List<string> GetKeys(string? prefix = default)
    {
        try
        {
            var keys = _server.Keys(_options.DefaultDatabase ?? -1, prefix);

            return keys.Select(key => key.ToString()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache GetKeys error for {Prefix}", prefix);

            return new List<string>();
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

    public async Task<T?> GetDefaultAsync<T>(string key, Func<Task<T?>>? provider = null, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(key)) return default;
        return provider is null ?
            await GetAsync(key, () => Task.FromResult(default(T)), expiry, ct) :
            await GetAsync(key, provider, expiry, ct);
    }

    public T? GetDefault<T>(string key, Func<T?>? provider = null, TimeSpan? expiry = null)
    {
        if (string.IsNullOrEmpty(key)) return default;
        return provider is null ?
             Get(key, () => default(T), expiry) :
             Get(key, provider, expiry);
    }
}

