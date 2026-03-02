using DotNetLibrary.Data.Contracts;
using DotNetLibrary.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DotNetLibrary.Data.Repository;

public class PermissionRepository : BaseRepository<Permission, int>, IPermissionRepository
{
    private readonly ILogger<PermissionRepository> _logger;

    public PermissionRepository(ApplicationDbContext context, ILogger<PermissionRepository> logger) : base(context)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<int>> GetExistingIdsAsync(IEnumerable<int> permissionIds, CancellationToken cancellationToken = default)
    {
        var distinctIds = permissionIds.Distinct().ToArray();
        if (distinctIds.Length == 0)
        {
            return Array.Empty<int>();
        }

        return await DbSet.AsNoTracking()
            .Where(p => !p.IsDeleted && distinctIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Permission>> GetLookupAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching permission lookup data");
        return await DbSet.AsNoTracking()
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }
}
