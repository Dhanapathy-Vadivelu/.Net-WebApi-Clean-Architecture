using DotNetLibrary.Domain.Contracts;
using DotNetLibrary.Shared.Payload.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotNetCoreWebApi.Controllers;

[AllowAnonymous]
[Route("oauth2")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly ITokenService _tokenService;

    public AuthController(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    [HttpPost("token")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> IssueToken([FromForm] OAuthTokenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.GrantType))
        {
            return BadRequest(new OAuthErrorResponse { Error = "invalid_request", ErrorDescription = "grant_type is required." });
        }

        (OAuthTokenResponse? token, OAuthErrorResponse? error, int statusCode) result = request.GrantType switch
        {
            "password" when !string.IsNullOrWhiteSpace(request.Username) && !string.IsNullOrWhiteSpace(request.Password)
                => await _tokenService.IssueByPasswordAsync(request.Username, request.Password, request.Scope, cancellationToken),
            "external" when !string.IsNullOrWhiteSpace(request.ExternalToken)
                => await _tokenService.IssueByExternalTokenAsync(request.ExternalToken, request.Scope, cancellationToken),
            "password" => (null, new OAuthErrorResponse { Error = "invalid_request", ErrorDescription = "username and password are required for password grant." }, StatusCodes.Status400BadRequest),
            "external" => (null, new OAuthErrorResponse { Error = "invalid_request", ErrorDescription = "external_token is required for external grant." }, StatusCodes.Status400BadRequest),
            _ => (null, new OAuthErrorResponse { Error = "unsupported_grant_type", ErrorDescription = "Supported grant types are password and external." }, StatusCodes.Status400BadRequest)
        };

        return result.error is not null
            ? StatusCode(result.statusCode, result.error)
            : StatusCode(result.statusCode, result.token);
    }
}
