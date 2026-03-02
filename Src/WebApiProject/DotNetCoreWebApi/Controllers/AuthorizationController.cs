using System.Security.Claims;
using DotNetLibrary.Domain.Contracts;
using DotNetLibrary.Shared.Payload.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DotNetCoreWebApi.Authorization.Services;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace DotNetCoreWebApi.Controllers;

[ApiController]
[Route("connect")]
public class AuthorizationController : ControllerBase
{
    private readonly IUserClaimsService _userClaimsService;
    private readonly ExternalIdTokenValidator _externalIdTokenValidator;

    public AuthorizationController(IUserClaimsService userClaimsService, ExternalIdTokenValidator externalIdTokenValidator)
    {
        _userClaimsService = userClaimsService;
        _externalIdTokenValidator = externalIdTokenValidator;
    }

    [HttpGet("authorize")]
    [HttpPost("authorize")]
    [AllowAnonymous]
    public async Task<IActionResult> AuthorizeEndpoint()
    {
        var authenticateResult = await HttpContext.AuthenticateAsync(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        if (authenticateResult?.Principal is null)
        {
            return Challenge(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        foreach (var claim in authenticateResult.Principal.Claims)
        {
            identity.AddClaim(claim);
        }

        var scopeParam = HttpContext.Request.Query["scope"].ToString();
        var requestedScopes = string.IsNullOrWhiteSpace(scopeParam)
            ? Array.Empty<string>()
            : scopeParam.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        identity.SetScopes(requestedScopes);
        identity.SetDestinations(static claim => claim.Type switch
        {
            Claims.Email => new[] { Destinations.AccessToken, Destinations.IdentityToken },
            Claims.GivenName => new[] { Destinations.AccessToken, Destinations.IdentityToken },
            Claims.FamilyName => new[] { Destinations.AccessToken, Destinations.IdentityToken },
            Claims.Name => new[] { Destinations.AccessToken, Destinations.IdentityToken },
            Claims.Role => new[] { Destinations.AccessToken },
            "permission" => new[] { Destinations.AccessToken },
            _ => new[] { Destinations.AccessToken }
        });

        var principal = new ClaimsPrincipal(identity);
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public IActionResult LogoutEndpoint()
    {
        // Delegated to OpenIddict; pass-through enabled.
        return SignOut(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpPost("token")]
    [AllowAnonymous]
    [Consumes("application/x-www-form-urlencoded")]
    [ProducesResponseType(typeof(OAuthTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OAuthErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TokenEndpoint([FromForm] OAuthTokenRequest oAuthTokenRequest, CancellationToken cancellationToken)
    {
        var grantType = oAuthTokenRequest.GrantType;

        var result = grantType switch
        {
            GrantTypes.Password => await HandlePasswordGrantAsync(oAuthTokenRequest, cancellationToken),
            GrantTypes.RefreshToken => await HandleRefreshTokenGrantAsync(),
            GrantTypes.ClientCredentials => await HandleClientCredentialsGrantAsync(oAuthTokenRequest),
            "external" => await HandleExternalGrantAsync(oAuthTokenRequest, cancellationToken),
            _ => BadRequest(new OAuthErrorResponse { Error = Errors.UnsupportedGrantType, ErrorDescription = "Grant type not supported." })
        };

        return result;
    }

    [HttpGet("userinfo")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<IActionResult> UserInfo()
    {
        var result = await HttpContext.AuthenticateAsync(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        var principal = result.Principal;
        if (principal is null)
        {
            return Forbid();
        }

        var response = new
        {
            sub = principal.GetClaim(Claims.Subject),
            name = principal.GetClaim(Claims.Name),
            given_name = principal.GetClaim(Claims.GivenName),
            family_name = principal.GetClaim(Claims.FamilyName),
            email = principal.GetClaim(Claims.Email),
            roles = principal.GetClaims(Claims.Role),
            permissions = principal.GetClaims("permission")
        };

        return Ok(response);
    }

    private ClaimsIdentity BuildIdentity(UserClaimsResult claimsResult, IEnumerable<string> requestedScopes)
    {
        var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        identity.AddClaim(Claims.Subject, claimsResult.User.Id.ToString());
        identity.AddClaim(Claims.Email, claimsResult.User.Email);
        identity.AddClaim(Claims.GivenName, claimsResult.User.FirstName);
        identity.AddClaim(Claims.FamilyName, claimsResult.User.LastName);

        foreach (var role in claimsResult.Roles)
        {
            identity.AddClaim(Claims.Role, role);
        }

        foreach (var permission in claimsResult.Permissions)
        {
            identity.AddClaim("permission", permission);
        }

        identity.SetScopes(requestedScopes);
        identity.SetDestinations(static claim => claim.Type switch
        {
            Claims.Email => new[] { Destinations.AccessToken, Destinations.IdentityToken },
            Claims.GivenName => new[] { Destinations.AccessToken, Destinations.IdentityToken },
            Claims.FamilyName => new[] { Destinations.AccessToken, Destinations.IdentityToken },
            Claims.Role => new[] { Destinations.AccessToken },
            "permission" => new[] { Destinations.AccessToken },
            _ => new[] { Destinations.AccessToken }
        });

        return identity;
    }

    private async Task<IActionResult> HandlePasswordGrantAsync(OAuthTokenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new OAuthErrorResponse { Error = Errors.InvalidRequest, ErrorDescription = "username and password are required." });
        }

        var claimsResult = await _userClaimsService.ValidateUserAsync(request.Username, request.Password, cancellationToken);
        if (claimsResult is null)
        {
            return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var principal = new ClaimsPrincipal(BuildIdentity(claimsResult, ParseScopes(request.Scope)));
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private async Task<IActionResult> HandleRefreshTokenGrantAsync()
    {
        var authenticateResult = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        if (!authenticateResult.Succeeded || authenticateResult.Principal is null)
        {
            return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        return SignIn(authenticateResult.Principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private Task<IActionResult> HandleClientCredentialsGrantAsync(OAuthTokenRequest request)
    {
        var clientId = HttpContext.Request.Form["client_id"].ToString();
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return Task.FromResult<IActionResult>(BadRequest(new OAuthErrorResponse { Error = Errors.InvalidRequest, ErrorDescription = "client_id is required." }));
        }

        var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        identity.AddClaim(Claims.Subject, clientId);
        identity.AddClaim(Claims.Name, clientId);
        identity.SetScopes(ParseScopes(request.Scope));
        identity.SetDestinations(static claim => claim.Type switch
        {
            Claims.Name => new[] { Destinations.AccessToken },
            Claims.Subject => new[] { Destinations.AccessToken },
            _ => new[] { Destinations.AccessToken }
        });

        return Task.FromResult<IActionResult>(SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme));
    }

    private async Task<IActionResult> HandleExternalGrantAsync(OAuthTokenRequest request, CancellationToken cancellationToken)
    {
        var provider = HttpContext.Request.Form["provider"].ToString();
        var idToken = HttpContext.Request.Form["id_token"].ToString();

        if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(idToken))
        {
            return BadRequest(new OAuthErrorResponse { Error = Errors.InvalidRequest, ErrorDescription = "provider and id_token are required for external grant." });
        }

        var externalPrincipal = await _externalIdTokenValidator.ValidateAsync(provider, idToken, cancellationToken);
        if (externalPrincipal is null)
        {
            return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var email = externalPrincipal.FindFirstValue(ClaimTypes.Email)
            ?? externalPrincipal.FindFirstValue("email")
            ?? externalPrincipal.FindFirstValue("preferred_username");

        if (string.IsNullOrWhiteSpace(email))
        {
            return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var claimsResult = await _userClaimsService.GetActiveUserClaimsByEmailAsync(email, cancellationToken);
        if (claimsResult is null)
        {
            return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var principal = new ClaimsPrincipal(BuildIdentity(claimsResult, ParseScopes(request.Scope)));
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static IEnumerable<string> ParseScopes(string? scope)
    {
        return string.IsNullOrWhiteSpace(scope)
            ? Array.Empty<string>()
            : scope.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
