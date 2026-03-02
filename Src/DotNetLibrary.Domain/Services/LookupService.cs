using DotNetLibrary.Data.Contracts;
using DotNetLibrary.Domain.Contracts;
using DotNetLibrary.Shared.Payload.Lookup;
using DotNetLibrary.Shared.Responses;
using Microsoft.AspNetCore.Http;

namespace DotNetLibrary.Domain.Services;

public class LookupService : ILookupService
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;

    public LookupService(IRoleRepository roleRepository, IPermissionRepository permissionRepository)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
    }

    public async Task<ApiResponse<IEnumerable<RoleLookupDto>>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _roleRepository.GetLookupAsync(cancellationToken);
        var roleLookup = roles.Select(r => new RoleLookupDto
        {
            Id = r.Id,
            Name = r.Name
        });

        return ApiResponse<IEnumerable<RoleLookupDto>>.Ok(roleLookup, statusCode: StatusCodes.Status200OK);
    }

    public async Task<ApiResponse<IEnumerable<PermissionLookupDto>>> GetPermissionsAsync(CancellationToken cancellationToken = default)
    {
        var permissions = await _permissionRepository.GetLookupAsync(cancellationToken);
        var permissionLookup = permissions.Select(p => new PermissionLookupDto
        {
            Id = p.Id,
            Name = p.Name,
            Code = p.Code,
            Description = p.Description
        });

        return ApiResponse<IEnumerable<PermissionLookupDto>>.Ok(permissionLookup, statusCode: StatusCodes.Status200OK);
    }
}
