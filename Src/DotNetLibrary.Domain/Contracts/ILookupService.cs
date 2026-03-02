using DotNetLibrary.Shared.Payload.Lookup;
using DotNetLibrary.Shared.Responses;

namespace DotNetLibrary.Domain.Contracts;

public interface ILookupService
{
    Task<ApiResponse<IEnumerable<RoleLookupDto>>> GetRolesAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<IEnumerable<PermissionLookupDto>>> GetPermissionsAsync(CancellationToken cancellationToken = default);
}
