using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Tuna.SuperTemplate.ApiDocs.Conventions;

public class ProblemDetailsConvention : IApplicationModelConvention
{
    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {
            foreach (var action in controller.Actions)
            {
                if (action.Attributes.Any(attr => attr is Microsoft.AspNetCore.Mvc.ProducesResponseTypeAttribute))
                    continue;

                action.Filters.Add(new Microsoft.AspNetCore.Mvc.ProducesResponseTypeAttribute(
                    typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status400BadRequest));

                action.Filters.Add(new Microsoft.AspNetCore.Mvc.ProducesResponseTypeAttribute(
                    typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status500InternalServerError));
            }
        }
    }
}
