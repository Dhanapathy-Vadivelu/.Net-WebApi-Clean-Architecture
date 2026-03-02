using DotNetLibrary.Shared.Payload.Lookup;
using DotNetLibrary.Shared.Responses;

namespace DotNetLibrary.Domain.Contracts;

public interface IRolePermissionService
{
    Task<ApiResponse<IEnumerable<PermissionLookupDto>>> GetByRoleIdAsync(int roleId, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> UpdateRolePermissionsAsync(int roleId, IReadOnlyCollection<int> permissionIds, CancellationToken cancellationToken = default);
}
