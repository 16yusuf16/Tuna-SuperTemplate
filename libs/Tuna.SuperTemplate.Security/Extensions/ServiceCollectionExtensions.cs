using Microsoft.Extensions.DependencyInjection;
using Tuna.SuperTemplate.Security.Interfaces;
using Tuna.SuperTemplate.Security.Models;
namespace Tuna.SuperTemplate.Security.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSuperSecurity(
           this IServiceCollection services,
           JwtOptions jwtOptions,
           string encryptionKey)
    {
        services.AddSingleton(jwtOptions);
        services.AddSingleton<IJwtService, JwtService>();
        services.AddSingleton<IEncryptionService>(_ => new EncryptionService(encryptionKey));
        services.AddSingleton<IPermissionService, PermissionService>();
        services.AddScoped<ISecurityContextAccessor, SecurityContextAccessor>();
        services.AddHttpContextAccessor();
        return services;
    }
}
