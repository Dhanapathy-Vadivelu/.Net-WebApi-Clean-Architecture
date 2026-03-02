using DotNetLibrary.Data.Contracts;
using DotNetLibrary.Domain.Contracts;
using DotNetLibrary.Shared.Payload.Lookup;
using DotNetLibrary.Shared.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DotNetLibrary.Domain.Services;

public class RolePermissionService : IRolePermissionService
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IRolePermissionMapRepository _rolePermissionMapRepository;
    private readonly ILogger<RolePermissionService> _logger;

    public RolePermissionService(
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IRolePermissionMapRepository rolePermissionMapRepository,
        ILogger<RolePermissionService> logger)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _rolePermissionMapRepository = rolePermissionMapRepository;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<PermissionLookupDto>>> GetByRoleIdAsync(int roleId, CancellationToken cancellationToken = default)
    {
        var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
        if (role is null || role.IsDeleted)
        {
            return ApiResponse<IEnumerable<PermissionLookupDto>>.Fail($"Role with id {roleId} was not found.", statusCode: StatusCodes.Status404NotFound);
        }

        var permissions = await _rolePermissionMapRepository.GetPermissionsByRoleIdAsync(roleId, cancellationToken);
        var data = permissions.Select(p => new PermissionLookupDto
        {
            Id = p.Id,
            Name = p.Name,
            Code = p.Code,
            Description = p.Description
        });

        return ApiResponse<IEnumerable<PermissionLookupDto>>.Ok(data, statusCode: StatusCodes.Status200OK);
    }

    public async Task<ApiResponse<bool>> UpdateRolePermissionsAsync(int roleId, IReadOnlyCollection<int> permissionIds, CancellationToken cancellationToken = default)
    {
        var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
        if (role is null || role.IsDeleted)
        {
            return ApiResponse<bool>.Fail($"Role with id {roleId} was not found.", statusCode: StatusCodes.Status404NotFound);
        }

        var normalizedIds = permissionIds.Where(x => x > 0).Distinct().ToArray();
        var existingIds = await _permissionRepository.GetExistingIdsAsync(normalizedIds, cancellationToken);
        var missingIds = normalizedIds.Except(existingIds).ToArray();

        if (missingIds.Length > 0)
        {
            return ApiResponse<bool>.Fail($"Invalid permission ids: {string.Join(",", missingIds)}", statusCode: StatusCodes.Status400BadRequest);
        }

        await _rolePermissionMapRepository.ReplaceRolePermissionsAsync(roleId, normalizedIds, cancellationToken);
        _logger.LogInformation("Updated role-permission mappings for role {RoleId}", roleId);

        return ApiResponse<bool>.Ok(true, "Role permissions updated successfully", StatusCodes.Status200OK);
    }
}
