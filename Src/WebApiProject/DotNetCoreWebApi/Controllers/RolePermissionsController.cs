using DotNetLibrary.Domain.Contracts;
using DotNetLibrary.Shared.Payload.Roles;
using DotNetLibrary.Shared.Responses;
using DotNetLibrary.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotNetCoreWebApi.Controllers;

[Route("api/roles/{roleId:int}/permissions")]
[ApiController]
public class RolePermissionsController : ControllerBase
{
    private readonly IRolePermissionService _rolePermissionService;

    public RolePermissionsController(IRolePermissionService rolePermissionService)
    {
        _rolePermissionService = rolePermissionService;
    }

    [HttpGet]
    [Authorize(Policy = PermissionCodes.RoleRead)]
    public async Task<IActionResult> GetPermissionsByRoleId([FromRoute] int roleId, CancellationToken cancellationToken)
    {
        var response = await _rolePermissionService.GetByRoleIdAsync(roleId, cancellationToken);
        return StatusCode(response.StatusCode ?? StatusCodes.Status200OK, response);
    }

    [HttpPut]
    [Authorize(Policy = PermissionCodes.RoleWrite)]
    public async Task<IActionResult> UpdateRolePermissions([FromRoute] int roleId, [FromBody] UpdateRolePermissionsRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return BadRequest(ApiResponse<bool>.Fail(errors, "Validation failed", StatusCodes.Status400BadRequest));
        }

        var response = await _rolePermissionService.UpdateRolePermissionsAsync(roleId, request.PermissionIds.Distinct().ToArray(), cancellationToken);
        return StatusCode(response.StatusCode ?? StatusCodes.Status200OK, response);
    }
}
