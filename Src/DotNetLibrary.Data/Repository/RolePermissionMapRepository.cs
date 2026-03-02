using DotNetLibrary.Data.Contracts;
using DotNetLibrary.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DotNetLibrary.Data.Repository;

public class RolePermissionMapRepository : BaseRepository<RolePermissionMap, int>, IRolePermissionMapRepository
{
    private readonly ILogger<RolePermissionMapRepository> _logger;

    public RolePermissionMapRepository(ApplicationDbContext context, ILogger<RolePermissionMapRepository> logger) : base(context)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyList<Permission>> GetPermissionsByRoleIdAsync(int roleId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching permissions for role {RoleId}", roleId);

        return await Context.RolePermissionMaps.AsNoTracking()
            .Where(x => x.RoleId == roleId && !x.IsDeleted)
            .Join(
                Context.Permissions.AsNoTracking().Where(p => !p.IsDeleted),
                map => map.PermissionId,
                permission => permission.Id,
                (_, permission) => permission)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task ReplaceRolePermissionsAsync(int roleId, IReadOnlyCollection<int> permissionIds, CancellationToken cancellationToken = default)
    {
        var rolePermissionMaps = await Context.RolePermissionMaps
            .Where(x => x.RoleId == roleId)
            .ToListAsync(cancellationToken);

        var existingPermissionIds = rolePermissionMaps
            .Where(x => !x.IsDeleted)
            .Select(x => x.PermissionId)
            .ToHashSet();

        var incomingIds = permissionIds.Distinct().ToHashSet();

        var toSoftDelete = rolePermissionMaps
            .Where(x => !x.IsDeleted && !incomingIds.Contains(x.PermissionId))
            .ToList();

        foreach (var item in toSoftDelete)
        {
            item.IsDeleted = true;
            item.UpdatedAt = DateTime.UtcNow;
        }

        foreach (var permissionId in incomingIds)
        {
            var deletedRow = rolePermissionMaps.FirstOrDefault(x => x.PermissionId == permissionId && x.IsDeleted);
            if (deletedRow is not null)
            {
                deletedRow.IsDeleted = false;
                deletedRow.UpdatedAt = DateTime.UtcNow;
                continue;
            }

            if (existingPermissionIds.Contains(permissionId))
            {
                continue;
            }

            await Context.RolePermissionMaps.AddAsync(new RolePermissionMap
            {
                RoleId = roleId,
                PermissionId = permissionId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = 0,
                IsDeleted = false
            }, cancellationToken);
        }

        await Context.SaveChangesAsync(cancellationToken);
    }
}
