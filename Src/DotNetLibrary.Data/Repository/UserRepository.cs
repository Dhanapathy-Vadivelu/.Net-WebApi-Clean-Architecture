using DotNetLibrary.Data.Contracts;
using DotNetLibrary.Data.Entities;
using DotNetLibrary.Shared.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DotNetLibrary.Data.Repository;

public class UserRepository : BaseRepository<User, int>, IUserRepository
{
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(ApplicationDbContext context, ILogger<UserRepository> logger) : base(context)
    {
        _logger = logger;
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching user by email {Email}", email);
        return await DbSet.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<User?> GetByEmailForUpdateAsync(string email, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching tracked user by email {Email}", email);
        return await DbSet.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<PagedResult<User>> GetUsersAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking().OrderBy(u => u.FirstName).ThenBy(u => u.LastName);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResult<User>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}
