using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Tuna.SuperTemplate.ApiDocs.Filters;

public class TraceIdSchemaFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= new List<IOpenApiParameter>();

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "TraceId",
            In = ParameterLocation.Header,
            Required = false,
            Description = "Telemetry Trace identifier"
        });
    }
}
