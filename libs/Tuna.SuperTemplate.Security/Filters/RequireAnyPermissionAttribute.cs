using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Tuna.SuperTemplate.Security.Interfaces;

namespace Tuna.SuperTemplate.Security.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class RequireAnyPermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string[] _permissions;

    public RequireAnyPermissionAttribute(params string[] permissions)
    {
        _permissions = permissions;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var accessor = context.HttpContext.RequestServices.GetRequiredService<ISecurityContextAccessor>();
        var permissionService = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();

        if (accessor.UserId is null)
        {
            context.Result = new ForbidResult();
            return;
        }

        foreach (var permission in _permissions)
        {
            if (await permissionService.HasPermissionAsync(accessor.UserId.Value, permission))
            {
                return; // en az biri varsa geç
            }
        }

        context.Result = new ForbidResult();
    }
}
