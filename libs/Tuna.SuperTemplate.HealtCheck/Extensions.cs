using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tuna.SuperTemplate.HealtCheck;

namespace Tuna.SuperTemplate.HealthCheck;

public static class Extensions
{
    public static IServiceCollection AddCustomHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IHealthChecksBuilder>? configureChecks = null)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));

        var healthOptions = configuration
            .GetSection(nameof(HealthOptions))
            .Get<HealthOptions>() ?? new HealthOptions();

        services.Configure<HealthOptions>(configuration.GetSection(nameof(HealthOptions)));

        var hcBuilder = services.AddHealthChecks();
        configureChecks?.Invoke(hcBuilder);

        //if (healthOptions.Enabled)
        //{
        //    var uiBuilder = services.AddHealthChecksUI(options =>
        //    {
        //        options.SetEvaluationTimeInSeconds(healthOptions.EvaluationInterval);
        //        options.MaximumHistoryEntriesPerEndpoint(healthOptions.MaxHistoryEntries);
        //        options.AddHealthCheckEndpoint("self", healthOptions.HealthEndpoint);
        //    });

        //    try
        //    {
        //        uiBuilder.AddInMemoryStorage();
        //    }
        //    catch (Exception)
        //    {
        //    }
        //}

        return services;
    }

    public static WebApplication UseCustomHealthCheck(this WebApplication app)
    {
        if (app is null) throw new ArgumentNullException(nameof(app));

        var healthOptions = app.Configuration
            .GetSection(nameof(HealthOptions))
            .Get<HealthOptions>() ?? new HealthOptions();

        app.MapHealthChecks(healthOptions.HealthEndpoint, new HealthCheckOptions
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        app.MapHealthChecks(healthOptions.AlivenessEndpoint, new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains(healthOptions.LivenessTag),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        //if (healthOptions.Enabled)
        //{
        //    app.MapHealthChecksUI(options => options.UIPath = healthOptions.UIPath);
        //}

        return app;
    }
}
