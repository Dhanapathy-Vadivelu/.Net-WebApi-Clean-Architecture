using DotNetLibrary.Data.Entities;

namespace DotNetLibrary.Data.Contracts;

public interface IPermissionRepository : IBaseRepository<Permission, int>
{
    Task<IReadOnlyList<Permission>> GetLookupAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<int>> GetExistingIdsAsync(IEnumerable<int> permissionIds, CancellationToken cancellationToken = default);
}
