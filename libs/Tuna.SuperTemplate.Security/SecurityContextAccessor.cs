using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Tuna.SuperTemplate.Security.Interfaces;

namespace Tuna.SuperTemplate.Security;

public class SecurityContextAccessor : ISecurityContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SecurityContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? UserId => Int32.TryParse(GetClaim(ClaimTypes.NameIdentifier), out var id) ? id : null;
    public string? UserName => GetClaim(ClaimTypes.Name);
    public string?[] Roles => _httpContextAccessor.HttpContext?.User.Claims
        .Where(c => c.Type == ClaimTypes.Role)
        .Select(c => c.Value)
        .ToArray() ?? Array.Empty<string>();

    public string? GetClaim(string claimType) =>
        _httpContextAccessor.HttpContext?.User?.FindFirst(claimType)?.Value;
}
