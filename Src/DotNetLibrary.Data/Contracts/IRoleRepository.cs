using DotNetLibrary.Data.Entities;

namespace DotNetLibrary.Data.Contracts;

public interface IRoleRepository : IBaseRepository<Role, int>
{
    Task<IReadOnlyList<Role>> GetLookupAsync(CancellationToken cancellationToken = default);
}
