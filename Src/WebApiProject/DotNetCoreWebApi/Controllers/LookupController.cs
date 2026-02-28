using DotNetLibrary.Domain.Contracts;
using DotNetLibrary.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotNetCoreWebApi.Controllers;

public class LookupController : ApiBaseController
{
    private readonly ILookupService _lookupService;

    public LookupController(ILookupService lookupService)
    {
        _lookupService = lookupService;
    }

    [HttpGet("roles")]
    [Authorize(Policy = PermissionCodes.RoleRead)]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
    {
        var response = await _lookupService.GetRolesAsync(cancellationToken);
        return StatusCode(response.StatusCode ?? StatusCodes.Status200OK, response);
    }

    [HttpGet("permissions")]
    [Authorize(Policy = PermissionCodes.RoleRead)]
    public async Task<IActionResult> GetPermissions(CancellationToken cancellationToken)
    {
        var response = await _lookupService.GetPermissionsAsync(cancellationToken);
        return StatusCode(response.StatusCode ?? StatusCodes.Status200OK, response);
    }
}
