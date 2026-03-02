using DotNetLibrary.Shared.Payload.Users;
using DotNetLibrary.Shared.Responses;

namespace DotNetLibrary.Domain.Contracts;

public interface IUserManagementService
{
    Task<ApiResponse<UserDto>> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserDto>> UpdateAsync(UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PagedResponse<UserDto>> GetUsersAsync(UserListRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> VerifyRegistrationOtpAsync(VerifyRegistrationOtpRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> ResendRegistrationOtpAsync(ResendRegistrationOtpRequest request, CancellationToken cancellationToken = default);
}
