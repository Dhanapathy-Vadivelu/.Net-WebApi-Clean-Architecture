using DotNetLibrary.Data.Contracts;
using DotNetLibrary.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DotNetLibrary.Data.Repository;

public class RoleRepository : BaseRepository<Role, int>, IRoleRepository
{
    private readonly ILogger<RoleRepository> _logger;

    public RoleRepository(ApplicationDbContext context, ILogger<RoleRepository> logger) : base(context)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyList<Role>> GetLookupAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching role lookup data");
        return await DbSet.AsNoTracking()
            .Where(r => !r.IsDeleted)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }
}
