using DotNetLibrary.Data.Entities;

namespace DotNetLibrary.Domain.Contracts;

public record UserClaimsResult(User User, IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions);

public interface IUserClaimsService
{
    Task<UserClaimsResult?> ValidateUserAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<UserClaimsResult?> GetActiveUserClaimsByEmailAsync(string email, CancellationToken cancellationToken = default);
}
