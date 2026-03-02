namespace DotNetLibrary.Shared.Security;

public class OAuthOptions
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; set; } = 60;
    public int RefreshTokenExpirationMinutes { get; set; } = 43200; // 30 days
    public string DefaultScope { get; set; } = "api";
    public ExternalIdpOptions ExternalIdp { get; set; } = new();
    public ExternalProvidersOptions ExternalProviders { get; set; } = new();
}

public class ExternalIdpOptions
{
    public bool Enabled { get; set; }
    public string Authority { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string? ValidIssuer { get; set; }
    public bool RequireHttpsMetadata { get; set; } = true;
}
