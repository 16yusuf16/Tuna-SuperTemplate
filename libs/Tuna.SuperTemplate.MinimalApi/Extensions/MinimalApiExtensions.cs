using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Scrutor;
using Tuna.SuperTemplate.MinimalApi.Interfaces;

namespace Tuna.SuperTemplate.MinimalApi.Extensions;

public static class MinimalApiExtensions
{
    public static IServiceCollection AddMinimalEndpoints(this IServiceCollection services, params Assembly[] scanAssemblies)
    {
        if (scanAssemblies.Length == 0)
            scanAssemblies = Assembly.GetExecutingAssembly().GetReferencedAssemblies()
                .Select(a => Assembly.Load(a))
                .ToArray();

        services.Scan(scan =>
            scan.FromAssemblies(scanAssemblies)
                .AddClasses(classes => classes.AssignableTo(typeof(IMinimalEndpoint)))
                .UsingRegistrationStrategy(Scrutor.RegistrationStrategy.Append)
                .As<IMinimalEndpoint>()
                .WithLifetime(ServiceLifetime.Scoped)
        );

        return services;
    }

    public static Microsoft.AspNetCore.Routing.IEndpointRouteBuilder MapMinimalEndpoints(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder builder)
    {
        using var scope = builder.ServiceProvider.CreateScope();
        var endpoints = scope.ServiceProvider.GetServices<IMinimalEndpoint>().ToList();

        foreach (var ep in endpoints)
            ep.MapEndpoint(builder);

        return builder;
    }
}