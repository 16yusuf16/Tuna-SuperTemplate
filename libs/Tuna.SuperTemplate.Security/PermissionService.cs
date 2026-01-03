using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tuna.SuperTemplate.Security.Interfaces;

namespace Tuna.SuperTemplate.Security;

public class PermissionService : IPermissionService
{
    // Mock storage — burada bir dictionary tutuyoruz.
    private readonly ConcurrentDictionary<int, List<string>> _permissions = new();

    public bool HasPermission(int userId, string permission)
    {
        if (_permissions.TryGetValue(userId, out var list))
            return list.Contains(permission, StringComparer.OrdinalIgnoreCase);

        return false;
    }

    public Task<bool> HasPermissionAsync(int userId, string permission)
        => Task.FromResult(HasPermission(userId, permission));

 
    public void GrantPermission(int userId, string permission)
    {
        _permissions.AddOrUpdate(
            userId,
            _ => new List<string> { permission },
            (_, list) =>
            {
                if (!list.Contains(permission))
                    list.Add(permission);
                return list;
            });
    }
}
