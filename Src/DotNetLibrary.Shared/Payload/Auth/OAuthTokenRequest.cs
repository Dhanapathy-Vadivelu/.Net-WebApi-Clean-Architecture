using Microsoft.AspNetCore.Mvc;

namespace DotNetLibrary.Shared.Payload.Auth;

public class OAuthTokenRequest
{
    [FromForm(Name = "grant_type")]
    public string GrantType { get; set; } = string.Empty;

    [FromForm(Name = "username")]
    public string? Username { get; set; }

    [FromForm(Name = "password")]
    public string? Password { get; set; }

    [FromForm(Name = "scope")]
    public string? Scope { get; set; }

    [FromForm(Name = "external_token")]
    public string? ExternalToken { get; set; }

    [FromForm(Name = "refresh_token")]
    public string? RefreshToken { get; set; }
}
