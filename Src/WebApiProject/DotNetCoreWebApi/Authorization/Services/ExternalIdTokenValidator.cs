using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DotNetLibrary.Shared.Security;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace DotNetCoreWebApi.Authorization.Services;

public class ExternalIdTokenValidator
{
    private readonly OAuthOptions _options;

    public ExternalIdTokenValidator(Microsoft.Extensions.Options.IOptions<OAuthOptions> options)
    {
        _options = options.Value;
    }

    public async Task<ClaimsPrincipal?> ValidateAsync(string provider, string idToken, CancellationToken cancellationToken)
    {
        return provider.ToLowerInvariant() switch
        {
            "google" => await ValidateGoogleAsync(idToken, cancellationToken),
            "microsoft" or "entra" => await ValidateMicrosoftAsync(idToken, cancellationToken),
            _ => null
        };
    }

    private async Task<ClaimsPrincipal?> ValidateGoogleAsync(string idToken, CancellationToken cancellationToken)
    {
        var google = _options.ExternalProviders.Google;
        if (string.IsNullOrWhiteSpace(google.ClientId))
        {
            return null;
        }

        var metadataAddress = "https://accounts.google.com/.well-known/openid-configuration";
        var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            metadataAddress,
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever { RequireHttps = google.RequireHttpsMetadata });

        var config = await configurationManager.GetConfigurationAsync(cancellationToken);
        var parameters = new TokenValidationParameters
        {
            ValidIssuer = "https://accounts.google.com",
            ValidAudience = google.ClientId,
            IssuerSigningKeys = config.SigningKeys,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.ValidateToken(idToken, parameters, out _);
    }

    private async Task<ClaimsPrincipal?> ValidateMicrosoftAsync(string idToken, CancellationToken cancellationToken)
    {
        var ms = _options.ExternalProviders.Microsoft;
        if (string.IsNullOrWhiteSpace(ms.TenantId) || string.IsNullOrWhiteSpace(ms.ClientId))
        {
            return null;
        }

        var metadataAddress = $"https://login.microsoftonline.com/{ms.TenantId}/v2.0/.well-known/openid-configuration";
        var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            metadataAddress,
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever { RequireHttps = ms.RequireHttpsMetadata });

        var config = await configurationManager.GetConfigurationAsync(cancellationToken);
        var parameters = new TokenValidationParameters
        {
            ValidIssuer = config.Issuer,
            ValidAudience = ms.ClientId,
            IssuerSigningKeys = config.SigningKeys,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.ValidateToken(idToken, parameters, out _);
    }
}
