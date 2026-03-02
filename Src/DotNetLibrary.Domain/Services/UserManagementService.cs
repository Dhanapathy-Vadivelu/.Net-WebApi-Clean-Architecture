using DotNetLibrary.Data.Contracts;
using DotNetLibrary.Data.Entities;
using DotNetLibrary.Core.NotificationProcessor.Contracts;
using DotNetLibrary.Core.NotificationProcessor.Models;
using DotNetLibrary.Domain.Contracts;
using DotNetLibrary.Shared.Payload.Users;
using DotNetLibrary.Shared.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace DotNetLibrary.Domain.Services;

public class UserManagementService : IUserManagementService
{
    private const string RegistrationOtpPurpose = "REGISTRATION";

    private readonly IUserRepository _userRepository;
    private readonly IUserOtpRepository _userOtpRepository;
    private readonly INotificationProcessor _notificationProcessor;
    private readonly ILogger<UserManagementService> _logger;
    private readonly IPasswordHasher<User> _passwordHasher;

    public UserManagementService(
        IUserRepository userRepository,
        IUserOtpRepository userOtpRepository,
        INotificationProcessor notificationProcessor,
        ILogger<UserManagementService> logger,
        IPasswordHasher<User> passwordHasher)
    {
        _userRepository = userRepository;
        _userOtpRepository = userOtpRepository;
        _notificationProcessor = notificationProcessor;
        _logger = logger;
        _passwordHasher = passwordHasher;
    }

    public async Task<ApiResponse<UserDto>> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var existingUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser is not null && existingUser.IsActive)
        {
            return ApiResponse<UserDto>.Fail($"User with email {request.Email} already exists.", statusCode: StatusCodes.Status409Conflict);
        }

        if (existingUser is null)
        {
            existingUser = new User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                IsActive = false,
                CreatedAt = DateTime.UtcNow,
            };

            existingUser.PasswordHash = _passwordHasher.HashPassword(existingUser, request.Password);

            await _userRepository.AddAsync(existingUser, cancellationToken);
            await SendRegistrationOtpAsync(existingUser, cancellationToken);

            _logger.LogInformation("Created user {UserId} with email {Email}", existingUser.Id, existingUser.Email);
        }
        else
        {
            await SendRegistrationOtpAsync(existingUser, cancellationToken);
        }
        
        return ApiResponse<UserDto>.Ok(ToDto(existingUser), "OTP has been sent to email for activation.", StatusCodes.Status201Created);
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var existing = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return ApiResponse<bool>.Fail($"User with id {id} was not found.", statusCode: StatusCodes.Status404NotFound);
        }

        await _userRepository.DeleteAsync(existing, cancellationToken);
        _logger.LogInformation("Deleted user {UserId}", id);
        return ApiResponse<bool>.Ok(true, "User deleted successfully", StatusCodes.Status200OK);
    }

    public async Task<ApiResponse<UserDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var existing = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return ApiResponse<UserDto>.Fail($"User with id {id} was not found.", statusCode: StatusCodes.Status404NotFound);
        }

        return ApiResponse<UserDto>.Ok(ToDto(existing), statusCode: StatusCodes.Status200OK);
    }

    public async Task<PagedResponse<UserDto>> GetUsersAsync(UserListRequest request, CancellationToken cancellationToken = default)
    {
        var paged = await _userRepository.GetUsersAsync(request.PageNumber, request.PageSize, cancellationToken);
        var items = paged.Items.Select(ToDto).ToList();
        return PagedResponse<UserDto>.Ok(items, paged.PageNumber, paged.PageSize, paged.TotalCount, "Users fetched successfully");
    }

    public async Task<ApiResponse<UserDto>> UpdateAsync(UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _userRepository.GetByIdAsync(request.Id, cancellationToken);
        if (existing is null)
        {
            return ApiResponse<UserDto>.Fail($"User with id {request.Id} was not found.", statusCode: StatusCodes.Status404NotFound);
        }

        var otherUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (otherUser is not null && otherUser.Id != request.Id)
        {
            return ApiResponse<UserDto>.Fail($"Another user with email {request.Email} already exists.", statusCode: StatusCodes.Status409Conflict);
        }

        existing.FirstName = request.FirstName;
        existing.LastName = request.LastName;
        existing.Email = request.Email;
        existing.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(existing, cancellationToken);

        _logger.LogInformation("Updated user {UserId}", existing.Id);

        return ApiResponse<UserDto>.Ok(ToDto(existing), "User updated successfully", StatusCodes.Status200OK);
    }

    public async Task<ApiResponse<bool>> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _userRepository.GetByIdAsync(request.Id, cancellationToken);
        if (existing is null)
        {
            return ApiResponse<bool>.Fail($"User with id {request.Id} was not found.", statusCode: StatusCodes.Status404NotFound);
        }

        var verification = _passwordHasher.VerifyHashedPassword(existing, existing.PasswordHash, request.CurrentPassword);
        if (verification == PasswordVerificationResult.Failed)
        {
            return ApiResponse<bool>.Fail("Current password is incorrect.", statusCode: StatusCodes.Status400BadRequest);
        }

        existing.PasswordHash = _passwordHasher.HashPassword(existing, request.NewPassword);
        existing.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(existing, cancellationToken);

        _logger.LogInformation("Changed password for user {UserId}", existing.Id);

        return ApiResponse<bool>.Ok(true, "Password changed successfully", StatusCodes.Status200OK);
    }

    public async Task<ApiResponse<bool>> VerifyRegistrationOtpAsync(VerifyRegistrationOtpRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailForUpdateAsync(request.Email, cancellationToken);
        if (user is null || user.IsDeleted)
        {
            return ApiResponse<bool>.Fail("User was not found.", statusCode: StatusCodes.Status404NotFound);
        }

        var latestOtp = await _userOtpRepository.GetLatestValidAsync(user.Id, RegistrationOtpPurpose, cancellationToken);
        if (latestOtp is null)
        {
            return ApiResponse<bool>.Fail("OTP is expired or invalid.", statusCode: StatusCodes.Status400BadRequest);
        }

        var requestOtpHash = ComputeSha256(request.Otp);
        if (!requestOtpHash.Equals(latestOtp.CodeHash, StringComparison.OrdinalIgnoreCase))
        {
            return ApiResponse<bool>.Fail("OTP is expired or invalid.", statusCode: StatusCodes.Status400BadRequest);
        }

        latestOtp.IsUsed = true;
        latestOtp.UsedAt = DateTime.UtcNow;
        latestOtp.UpdatedAt = DateTime.UtcNow;

        await _userOtpRepository.UpdateAsync(latestOtp, cancellationToken);

        user.IsActive = true;
        user.EmailVerifiedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user, cancellationToken);
        return ApiResponse<bool>.Ok(true, "User email verified successfully.", StatusCodes.Status200OK);
    }

    public async Task<ApiResponse<bool>> ResendRegistrationOtpAsync(ResendRegistrationOtpRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailForUpdateAsync(request.Email, cancellationToken);
        if (user is null || user.IsDeleted)
        {
            return ApiResponse<bool>.Fail("User was not found.", statusCode: StatusCodes.Status404NotFound);
        }

        if (user.IsActive)
        {
            return ApiResponse<bool>.Fail("User is already active.", statusCode: StatusCodes.Status400BadRequest);
        }

        await SendRegistrationOtpAsync(user, cancellationToken);
        return ApiResponse<bool>.Ok(true, "OTP has been resent to email.", StatusCodes.Status200OK);
    }

    private async Task SendRegistrationOtpAsync(User user, CancellationToken cancellationToken)
    {
        await _userOtpRepository.InvalidatePendingAsync(user.Id, RegistrationOtpPurpose, cancellationToken);

        var otp = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        var otpEntity = new UserOtp
        {
            UserId = user.Id,
            Purpose = RegistrationOtpPurpose,
            CodeHash = ComputeSha256(otp),
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        await _userOtpRepository.AddAsync(otpEntity, cancellationToken);

        var content = $"Hi {user.FirstName},<br/><br/>Your OTP for account activation is <b>{otp}</b>.<br/>This OTP is valid for 10 minutes.";
        await _notificationProcessor.SendAsync(new NotificationMessage
        {
            Medium = CommunicationMedium.Email,
            To = user.Email,
            Subject = "Account Verification OTP",
            Content = content
        }, cancellationToken);
    }

    private static string ComputeSha256(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    private static UserDto ToDto(User user) => new()
    {
        Id = user.Id,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt
    };
}
