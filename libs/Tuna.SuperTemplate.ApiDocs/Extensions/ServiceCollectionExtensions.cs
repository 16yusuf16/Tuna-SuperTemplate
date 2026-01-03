using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Tuna.SuperTemplate.ApiDocs.Conventions;
using Tuna.SuperTemplate.ApiDocs.Filters;
using Tuna.SuperTemplate.ApiDocs.Versioning;

namespace Tuna.SuperTemplate.OpenApiDocs.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenApiDocs(
        this IServiceCollection services,
        string apiTitle,
        string version = "v1",
        bool enableVersioning = true)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(version, new OpenApiInfo
            {
                Title = apiTitle,
                Version = version
            });

            var bearerScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Scheme = "bearer",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme."
            };

            options.AddSecurityDefinition("Bearer", bearerScheme);
            options.OperationFilter<TraceIdSchemaFilter>();
            options.CustomOperationIds(api => api.ActionDescriptor.RouteValues["action"]);
        });

       // services.AddOpenApiDocs();

        // Default response ve problem details convention’ları
        services.Configure<MvcOptions>(options =>
        {
            options.Conventions.Add(new DefaultResponseConvention());
            options.Conventions.Add(new ProblemDetailsConvention());
        });

        if (enableVersioning)
        {
            services.AddApiVersioningWithExplorer();
        }

        return services;
    }
}
