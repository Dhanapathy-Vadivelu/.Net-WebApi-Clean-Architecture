using System.Text.Json.Serialization;

namespace DotNetLibrary.Shared.Payload.Auth;

public class OAuthErrorResponse
{
    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;

    [JsonPropertyName("error_description")]
    public string ErrorDescription { get; set; } = string.Empty;
}
