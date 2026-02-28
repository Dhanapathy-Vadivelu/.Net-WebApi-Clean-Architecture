using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DotNetLibrary.Data;
using DotNetLibrary.Data.Entities;
using DotNetLibrary.Domain.Contracts;
using DotNetLibrary.Shared.Payload.Auth;
using DotNetLibrary.Shared.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace DotNetLibrary.Domain.Services;

public class TokenService : ITokenService
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly OAuthOptions _oauthOptions;
    private readonly ILogger<TokenService> _logger;

    public TokenService(
        ApplicationDbContext context,
        IPasswordHasher<User> passwordHasher,
        IOptions<OAuthOptions> oauthOptions,
        ILogger<TokenService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _oauthOptions = oauthOptions.Value;
        _logger = logger;
    }

    public async Task<(OAuthTokenResponse? Token, OAuthErrorResponse? Error, int StatusCode)> IssueByPasswordAsync(string username, string password, string? scope, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == username && !x.IsDeleted && x.IsActive, cancellationToken);
        if (user is null)
        {
            return InvalidGrant("Invalid user credentials.");
        }

        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return InvalidGrant("Invalid user credentials.");
        }

        var token = await BuildAccessTokenAsync(user, scope, cancellationToken);
        _logger.LogInformation("Issued OAuth token using resource owner credentials for user {UserId}", user.Id);

        return (token, null, StatusCodes.Status200OK);
    }

    public async Task<(OAuthTokenResponse? Token, OAuthErrorResponse? Error, int StatusCode)> IssueByExternalTokenAsync(string externalToken, string? scope, CancellationToken cancellationToken = default)
    {
        if (!_oauthOptions.ExternalIdp.Enabled)
        {
            return InvalidRequest("External identity provider flow is disabled.");
        }

        var principal = await ValidateExternalTokenAsync(externalToken, cancellationToken);
        if (principal is null)
        {
            return InvalidGrant("Invalid external token.");
        }

        var email = principal.FindFirst(ClaimTypes.Email)?.Value
            ?? principal.FindFirst("email")?.Value
            ?? principal.FindFirst("preferred_username")?.Value;

        if (string.IsNullOrWhiteSpace(email))
        {
            return InvalidGrant("External token does not contain an email claim.");
        }

        var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == email && !x.IsDeleted && x.IsActive, cancellationToken);
        if (user is null)
        {
            return InvalidGrant("No local user mapping found for external identity.");
        }

        var token = await BuildAccessTokenAsync(user, scope, cancellationToken);
        _logger.LogInformation("Issued OAuth token using external identity provider for user {UserId}", user.Id);

        return (token, null, StatusCodes.Status200OK);
    }

    private async Task<OAuthTokenResponse> BuildAccessTokenAsync(User user, string? scope, CancellationToken cancellationToken)
    {
        var roles = await _context.UserRoleMaps.AsNoTracking()
            .Where(map => map.UserId == user.Id && !map.IsDeleted)
            .Join(
                _context.Roles.AsNoTracking().Where(role => !role.IsDeleted),
                map => map.RoleId,
                role => role.Id,
                (_, role) => role.Name)
            .Distinct()
            .ToListAsync(cancellationToken);

        var permissions = await _context.UserRoleMaps.AsNoTracking()
            .Where(userRole => userRole.UserId == user.Id && !userRole.IsDeleted)
            .Join(
                _context.RolePermissionMaps.AsNoTracking().Where(rolePermission => !rolePermission.IsDeleted),
                userRole => userRole.RoleId,
                rolePermission => rolePermission.RoleId,
                (_, rolePermission) => rolePermission.PermissionId)
            .Join(
                _context.Permissions.AsNoTracking().Where(permission => !permission.IsDeleted),
                permissionId => permissionId,
                permission => permission.Id,
                (_, permission) => permission.Code)
            .Distinct()
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_oauthOptions.AccessTokenExpirationMinutes);
        var issuedScope = string.IsNullOrWhiteSpace(scope) ? _oauthOptions.DefaultScope : scope;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new(JwtRegisteredClaimNames.FamilyName, user.LastName),
            new("scope", issuedScope)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(permissions.Select(permission => new Claim("permission", permission)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_oauthOptions.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _oauthOptions.Issuer,
            Audience = _oauthOptions.Audience,
            NotBefore = now,
            Expires = expires,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);

        return new OAuthTokenResponse
        {
            AccessToken = tokenHandler.WriteToken(securityToken),
            ExpiresIn = (int)TimeSpan.FromMinutes(_oauthOptions.AccessTokenExpirationMinutes).TotalSeconds,
            Scope = issuedScope
        };
    }

    private async Task<ClaimsPrincipal?> ValidateExternalTokenAsync(string externalToken, CancellationToken cancellationToken)
    {
        try
        {
            var external = _oauthOptions.ExternalIdp;
            var metadataAddress = $"{external.Authority.TrimEnd('/')}/.well-known/openid-configuration";

            var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                metadataAddress,
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever { RequireHttps = external.RequireHttpsMetadata });

            var openIdConfig = await configurationManager.GetConfigurationAsync(cancellationToken);
            var validIssuer = string.IsNullOrWhiteSpace(external.ValidIssuer) ? external.Authority : external.ValidIssuer;

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = validIssuer,
                ValidateAudience = true,
                ValidAudience = external.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = openIdConfig.SigningKeys,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(2)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            return tokenHandler.ValidateToken(externalToken, validationParameters, out _);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "External token validation failed.");
            return null;
        }
    }

    private static (OAuthTokenResponse? Token, OAuthErrorResponse? Error, int StatusCode) InvalidGrant(string description)
    {
        return (null, new OAuthErrorResponse { Error = "invalid_grant", ErrorDescription = description }, StatusCodes.Status400BadRequest);
    }

    private static (OAuthTokenResponse? Token, OAuthErrorResponse? Error, int StatusCode) InvalidRequest(string description)
    {
        return (null, new OAuthErrorResponse { Error = "invalid_request", ErrorDescription = description }, StatusCodes.Status400BadRequest);
    }
}
