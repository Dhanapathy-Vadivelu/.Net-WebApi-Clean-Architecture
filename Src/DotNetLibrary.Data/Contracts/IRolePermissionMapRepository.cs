using DotNetLibrary.Data.Entities;

namespace DotNetLibrary.Data.Contracts;

public interface IRolePermissionMapRepository : IBaseRepository<RolePermissionMap, int>
{
    Task<IReadOnlyList<Permission>> GetPermissionsByRoleIdAsync(int roleId, CancellationToken cancellationToken = default);
    Task ReplaceRolePermissionsAsync(int roleId, IReadOnlyCollection<int> permissionIds, CancellationToken cancellationToken = default);
}
