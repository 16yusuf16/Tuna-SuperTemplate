using Microsoft.AspNetCore.Builder;
using Scalar.AspNetCore;
namespace Tuna.SuperTemplate.ApiDocs.Extensions;

public static class ApplicationBuilderExtensions
{
    public static WebApplication UseOpenApiDocs(this WebApplication app)
    {
        if (app is null) throw new ArgumentNullException(nameof(app));

        app.UseSwagger();

        app.UseSwaggerUI(options =>
        {
            options.RoutePrefix = "swagger";
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Tuna SuperTemplate API v1");
        });

        app.MapScalarApiReference("/docs", options =>
        {
            options.WithTitle("Tuna SuperTemplate API Documentation");
            options.WithOpenApiRoutePattern("/swagger/v1/swagger.json");
        });

        return app;
    }
}
