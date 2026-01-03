using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Serilog;
using Tuna.SuperTemplate.Logging.Interface;

namespace Tuna.SuperTemplate.Logging;

public static class LoggingExtensions
{
    public static IServiceCollection AddCentralizedLogging(this IServiceCollection services, IConfiguration? configuration = null)
    {
        if (Log.Logger == null || Log.Logger.GetType().Name == "SilentLogger")
        {
            var cfg = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .MinimumLevel.Debug();

            if (configuration != null)
            {
                cfg = cfg.ReadFrom.Configuration(configuration);
            }

            cfg = cfg.WriteTo.Console();

            Log.Logger = cfg.CreateLogger();
        }

        services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));
        services.TryAddTransient(typeof(IAppLogger<>), typeof(AppLogger<>));

        return services;
    }
    public static IHostBuilder UseCentralizedLogging(this IHostBuilder hostBuilder)
    {
        hostBuilder.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.Console();
        });

        return hostBuilder;
    }
}
