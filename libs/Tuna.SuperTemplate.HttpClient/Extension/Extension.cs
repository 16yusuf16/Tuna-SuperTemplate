using Microsoft.Extensions.DependencyInjection;
using Tuna.SuperTemplate.HttpClient.Interface;

namespace Tuna.SuperTemplate.HttpClient.Extension;

public static class Extension
{
    public static IServiceCollection AddHttpClientService(this IServiceCollection services)
    {
        services.AddSingleton<IHttpClient,HttpClient>();
        return services;
    }
}
