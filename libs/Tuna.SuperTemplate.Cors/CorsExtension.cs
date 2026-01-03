using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;

namespace Tuna.SuperTemplate.Cors;

public static class CorsExtension
{
    private const string AllowCustomCorsPolicy = "AllowCustomPolicy";
    private const string CorsSectionName = "CorsOptions";

    public static IServiceCollection AddDefaultCors(this IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));
        if (env is null) throw new ArgumentNullException(nameof(env));

        if (env.IsDevelopment() || env.IsEnvironment("Test"))
        {
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
            });

            return services;
        }

        // Production-like: read allowed origins from configuration and create a named policy
        var allowedOrigins = configuration.GetSection($"{CorsSectionName}:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

        // Normalize and filter invalid/empty entries
        var normalizedOrigins = allowedOrigins
            .Where(o => !string.IsNullOrWhiteSpace(o))
            .Select(o => o.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        services.AddCors(options =>
        {
            options.AddPolicy(AllowCustomCorsPolicy, policyBuilder =>
            {
                if (normalizedOrigins.Length == 0)
                {
                    // If no origins configured, default to deny-cross-origin (or allow any if you prefer)
                    // Here we choose to allow no origins explicitly by not calling AllowAnyOrigin or WithOrigins.
                    // To opt-in to allow-any-origin instead, replace the following line with: policyBuilder.AllowAnyOrigin();
                    policyBuilder.DisallowCredentials(); // no-op for policy composition but signals intent
                }
                else if (normalizedOrigins.Length == 1 && normalizedOrigins[0] == "*")
                {
                    // "*" is a common config shortcut — treat it as allow any origin
                    policyBuilder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                }
                else
                {
                    policyBuilder.WithOrigins(normalizedOrigins).AllowAnyHeader().AllowAnyMethod();
                }
            });
        });

        return services;
    }
    // Call from Program.cs after building the app:
    // app.UseDefaultCors();
    public static IApplicationBuilder UseDefaultCors(this IApplicationBuilder app)
    {
        if (app is null) throw new ArgumentNullException(nameof(app));

        var env = app.ApplicationServices.GetService<IHostEnvironment>();
        if (env != null && (env.IsDevelopment() || env.IsEnvironment("Test")))
        {
            app.UseCors(); // default policy (development/test)
        }
        else
        {
            app.UseCors(AllowCustomCorsPolicy);
        }

        return app;
    }
}