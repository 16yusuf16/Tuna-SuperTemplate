using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Tuna.SuperTemplate.HybridCache.Hybrid;

namespace Tuna.SuperTemplate.HybridCache.Extensions;

public static class CacheServiceExtension
{
    public static IServiceCollection UseCaching(this IServiceCollection services, IConfiguration configuration)
    {
        var settings  = services.BindSettings<CacheSettings>(configuration);
        if(settings.Type is CacheProvider.Redis or CacheProvider.Hybrid)
        {
            var redisSettings = services.BindSettings<Tuna.SuperTemplate.HybridCache.Redis.RedisServerSettings>(configuration,true);
            var connectionParts = redisSettings.ConnectionString?.Split(':');
            if (connectionParts == null || connectionParts.Length != 2)
            {
                throw new ArgumentException("Invalid Redis connection string format. Expected format: host:port", nameof(redisSettings.ConnectionString));
            }

            string host = connectionParts[0];
            if (!int.TryParse(connectionParts[1], out int port))
            {
                throw new ArgumentException("Invalid port number in Redis connection string", nameof(redisSettings.ConnectionString));
            }

            var options = new ConfigurationOptions
            {
                AbortOnConnectFail = redisSettings.AbortOnConnectFail,
                AsyncTimeout = redisSettings.AsyncTimeoutMilliSecond,
                ConnectTimeout = redisSettings.ConnectTimeoutMilliSecond,
                Password = redisSettings.Password,
                AllowAdmin = redisSettings.AllowAdmin,
                DefaultDatabase = redisSettings.DefaultDatabase,
                EndPoints = { { host, port } }
            };


            ConnectionMultiplexer connection = ConnectionMultiplexer.Connect(options);
            services.AddSingleton(connection);
        }
        if(settings.Type == CacheProvider.Memory)
        {
            services.AddMemoryCache();
            services.AddSingleton<ICacheManager, MemoryCacheManager>();
            services.AddSingleton<IMemoryCacheManager, MemoryCacheManager>();

        }
        else if(settings.Type == CacheProvider.Redis)
        {
            services.AddSingleton<ICacheManager, Hybrid.RedisCacheManager>();
            services.AddSingleton<IDistributedCacheManager, Hybrid.RedisCacheManager>();
        }
        else if(settings.Type == CacheProvider.Hybrid)
        {
            services.AddMemoryCache();
       
            services.AddSingleton<IMemoryCacheManager, MemoryCacheManager>();
            services.AddSingleton<IDistributedCacheManager, Hybrid.RedisCacheManager>();
            services.AddSingleton<ICacheManager, HybridCacheManager>();
            services.AddHostedService<CacheSynchronizer>();
        }
        return services;
    }
    public static T BindSettings<T>(this IServiceCollection services, IConfiguration configuration, bool register = false)
        where T:class, new()
    {
        T settings = new();
        var section = configuration.GetSection(typeof(T).Name);
        section.Bind(settings);
        if (register)
        {
            services.AddSingleton(settings);
            services.Configure<T>(section);
        }
        return settings;
    }
}
