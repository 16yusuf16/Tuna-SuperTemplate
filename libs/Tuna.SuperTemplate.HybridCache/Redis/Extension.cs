using Microsoft.Extensions.DependencyInjection;

namespace Tuna.SuperTemplate.HybridCache.Redis;

public static class Extension
{
    public static IServiceCollection AddRedisCache(this IServiceCollection services)
    {
        return services.AddSingleton<IRedisCacheManager, RedisCacheManager>();
    }
}
