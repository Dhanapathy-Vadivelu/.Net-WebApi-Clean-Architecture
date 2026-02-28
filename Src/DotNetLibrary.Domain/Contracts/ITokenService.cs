using DotNetLibrary.Shared.Payload.Auth;

namespace DotNetLibrary.Domain.Contracts;

public interface ITokenService
{
    Task<(OAuthTokenResponse? Token, OAuthErrorResponse? Error, int StatusCode)> IssueByPasswordAsync(string username, string password, string? scope, CancellationToken cancellationToken = default);
    Task<(OAuthTokenResponse? Token, OAuthErrorResponse? Error, int StatusCode)> IssueByExternalTokenAsync(string externalToken, string? scope, CancellationToken cancellationToken = default);
}
