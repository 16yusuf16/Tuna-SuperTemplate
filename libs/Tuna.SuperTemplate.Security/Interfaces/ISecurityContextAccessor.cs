namespace Tuna.SuperTemplate.Security.Interfaces;

public interface ISecurityContextAccessor
{
    int? UserId { get; }
    string? UserName { get; }
    string?[] Roles { get; }
    string? GetClaim(string claimType);
}
