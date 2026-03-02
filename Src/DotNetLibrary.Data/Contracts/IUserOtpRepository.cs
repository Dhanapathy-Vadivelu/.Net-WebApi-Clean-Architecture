using DotNetLibrary.Data.Entities;

namespace DotNetLibrary.Data.Contracts;

public interface IUserOtpRepository : IBaseRepository<UserOtp, int>
{
    Task<UserOtp?> GetLatestValidAsync(int userId, string purpose, CancellationToken cancellationToken = default);
    Task InvalidatePendingAsync(int userId, string purpose, CancellationToken cancellationToken = default);
}
