using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Tuna.SuperTemplate.HybridCache.Constants;
using static StackExchange.Redis.RedisChannel;

namespace Tuna.SuperTemplate.HybridCache.Hybrid;

public class CacheSynchronizer : IHostedService
{
    #region Fields

    readonly CacheSettings _settings;
    readonly IMemoryCacheManager _primary;
    readonly IDistributedCacheManager _secondary;
    private readonly ConnectionMultiplexer _conn;
    ISubscriber Subscriber => _conn.GetSubscriber();
    private readonly ILogger<CacheSynchronizer> _logger;

    #endregion

    public CacheSynchronizer(IMemoryCacheManager primaryCache, IDistributedCacheManager secondaryCache, ConnectionMultiplexer conn, CacheSettings settings, ILogger<CacheSynchronizer> logger)
    {
        _conn = conn;
        _settings = settings;
        _logger = logger;
        _primary = primaryCache;
        _secondary = secondaryCache;

        _secondary.CacheUpdated += OnSecondaryCacheUpdatedOrRemoved;
        _secondary.CacheRemoved += OnSecondaryCacheUpdatedOrRemoved;
        _secondary.CacheCleared += OnSecondaryCacheCleared;

        Subscriber.Subscribe(new RedisChannel(_settings.RemoveSyncChannel, PatternMode.Literal), async (c, m) => await OnRemoveSynced(m));
        Subscriber.Subscribe(new RedisChannel(_settings.ClearSyncChannel, PatternMode.Literal), async (c, m) => await OnClearSynced(m));
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    #region Pub/Sub

    private async Task OnRemoveSynced(RedisValue msg)
    {
        var (ok, instanceId, keys) = ParseMessage(msg);
        if (ok && !_secondary.InstanceId.Equals(instanceId) && keys != null)
        {
            _logger.LogDebug("CacheSynchronizer {Msg} removing and synced.", msg);

            foreach (var key in keys)
            {
                await _primary.RemoveAsync(key);
            }
        }
    }

    private async Task OnClearSynced(RedisValue msg)
    {
        var (ok, instanceId, _) = ParseMessage(msg);
        if (ok && !_secondary.InstanceId.Equals(instanceId))
        {
            _logger.LogDebug("CacheSynchronizer {Msg} clearing and synced.", msg);

            await _primary.ClearAsync(default);
        }
    }

    #endregion

    #region Secondary Cache Events

    private void OnSecondaryCacheUpdatedOrRemoved(object? sender, string key)
    {
        if (sender is ICacheManager manager)
        {
            var msg = $"{manager.InstanceId}{Cache.Seperator}{key}";

            Subscriber.Publish(new RedisChannel(_settings.RemoveSyncChannel, PatternMode.Literal), msg);
        }
    }

    private void OnSecondaryCacheCleared(object? sender, EventArgs e)
    {
        if (sender is ICacheManager manager)
        {
            var msg = $"{manager.InstanceId}";

            Subscriber.Publish(new RedisChannel(_settings.ClearSyncChannel, PatternMode.Literal), msg);
        }
    }

    private static (bool, string, string[]?) ParseMessage(RedisValue msg)
    {
        if (!msg.HasValue) return (false, string.Empty, null);

        var parts = msg.ToString().Split(Cache.Seperator, StringSplitOptions.RemoveEmptyEntries);

        switch (parts.Length)
        {
            case 1:
                return (true, parts[0], null);
            case 2:
                return (true, parts[0], parts[1].Split(Cache.Seperator, StringSplitOptions.RemoveEmptyEntries));
            default:
                return (false, string.Empty, null);
        }
    }

    #endregion
}
