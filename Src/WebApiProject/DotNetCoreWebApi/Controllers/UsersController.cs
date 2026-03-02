using DotNetLibrary.Domain.Contracts;
using DotNetLibrary.Shared.Security;
using DotNetLibrary.Shared.Payload.Users;
using DotNetLibrary.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotNetCoreWebApi.Controllers;

public class UsersController : ApiBaseController
{
    private readonly IUserManagementService _userService;

    public UsersController(IUserManagementService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicyNames.AdminOnly)]
    public async Task<IActionResult> GetUsers([FromQuery] UserListRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<IEnumerable<UserDto>>.Fail(ModelErrors(), "Validation failed", StatusCodes.Status400BadRequest));
        }

        var response = await _userService.GetUsersAsync(request, cancellationToken);
        return StatusCode(response.StatusCode ?? StatusCodes.Status200OK, response);
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.AdminOrSelf)]
    public async Task<IActionResult> GetUserById([FromRoute] int id, CancellationToken cancellationToken)
    {
        var response = await _userService.GetByIdAsync(id, cancellationToken);
        var statusCode = response.StatusCode ?? (response.Success ? StatusCodes.Status200OK : StatusCodes.Status404NotFound);
        return StatusCode(statusCode, response);
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<UserDto>.Fail(ModelErrors(), "Validation failed", StatusCodes.Status400BadRequest));
        }

        var response = await _userService.CreateAsync(request, cancellationToken);
        var statusCode = response.StatusCode ?? (response.Success ? StatusCodes.Status201Created : StatusCodes.Status400BadRequest);
        return StatusCode(statusCode, response);
    }

    [HttpPost("verify-registration-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyRegistrationOtp([FromBody] VerifyRegistrationOtpRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<bool>.Fail(ModelErrors(), "Validation failed", StatusCodes.Status400BadRequest));
        }

        var response = await _userService.VerifyRegistrationOtpAsync(request, cancellationToken);
        return StatusCode(response.StatusCode ?? StatusCodes.Status200OK, response);
    }

    [HttpPost("resend-registration-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendRegistrationOtp([FromBody] ResendRegistrationOtpRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<bool>.Fail(ModelErrors(), "Validation failed", StatusCodes.Status400BadRequest));
        }

        var response = await _userService.ResendRegistrationOtpAsync(request, cancellationToken);
        return StatusCode(response.StatusCode ?? StatusCodes.Status200OK, response);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.AdminOrSelf)]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        if (id != request.Id)
        {
            ModelState.AddModelError(nameof(request.Id), "Route id does not match payload id.");
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<UserDto>.Fail(ModelErrors(), "Validation failed", StatusCodes.Status400BadRequest));
        }

        var response = await _userService.UpdateAsync(request, cancellationToken);
        var statusCode = response.StatusCode ?? (response.Success ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest);
        return StatusCode(statusCode, response);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.AdminOrSelf)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken)
    {
        var response = await _userService.DeleteAsync(id, cancellationToken);
        var statusCode = response.StatusCode ?? (response.Success ? StatusCodes.Status200OK : StatusCodes.Status404NotFound);
        return StatusCode(statusCode, response);
    }

    [HttpPost("{id:int}/change-password")]
    [Authorize(Policy = AuthorizationPolicyNames.AdminOrSelf)]
    public async Task<IActionResult> ChangePassword([FromRoute] int id, [FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        if (id != request.Id)
        {
            ModelState.AddModelError(nameof(request.Id), "Route id does not match payload id.");
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<bool>.Fail(ModelErrors(), "Validation failed", StatusCodes.Status400BadRequest));
        }

        var response = await _userService.ChangePasswordAsync(request, cancellationToken);
        var statusCode = response.StatusCode ?? (response.Success ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest);
        return StatusCode(statusCode, response);
    }

    private IEnumerable<string> ModelErrors()
    {
        return ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
    }
}
