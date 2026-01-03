namespace Tuna.SuperTemplate.Security.Interfaces;

public interface IPermissionService
{
    bool HasPermission(int userId, string permission);
    Task<bool> HasPermissionAsync(int userId, string permission);
}
