using DotNetLibrary.Data.Contracts;
using DotNetLibrary.Data.Entities;
using DotNetLibrary.Domain.Contracts;
using Microsoft.AspNetCore.Identity;

namespace DotNetLibrary.Domain.Services;

public class UserClaimsService : IUserClaimsService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher<User> _passwordHasher;

    public UserClaimsService(
        IUserRepository userRepository,
        IPasswordHasher<User> passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<UserClaimsResult?> ValidateUserAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetActiveUserWithClaimsByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return null;
        }

        return BuildUserClaims(user);
    }

    public async Task<UserClaimsResult?> GetActiveUserClaimsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetActiveUserWithClaimsByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            return null;
        }

        return BuildUserClaims(user);
    }

    private UserClaimsResult BuildUserClaims(User user)
    {
        var roles = user.UserRoleMaps
            .Where(ur => !ur.IsDeleted && ur.Role is { IsDeleted: false })
            .Select(ur => ur.Role.Name)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var permissions = user.UserRoleMaps
            .Where(ur => !ur.IsDeleted && ur.Role is { IsDeleted: false })
            .SelectMany(ur => ur.Role.RolePermissionMaps)
            .Where(rp => !rp.IsDeleted && rp.Permission is { IsDeleted: false })
            .Select(rp => rp.Permission.Code)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return new UserClaimsResult(user, roles, permissions);
    }
}
