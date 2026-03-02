using DotNetLibrary.Data.Contracts;
using DotNetLibrary.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotNetLibrary.Data.Repository;

public class UserOtpRepository : BaseRepository<UserOtp, int>, IUserOtpRepository
{
    public UserOtpRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<UserOtp?> GetLatestValidAsync(int userId, string purpose, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await DbSet
            .Where(x => x.UserId == userId && x.Purpose == purpose && !x.IsUsed && !x.IsDeleted && x.ExpiresAt >= now)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task InvalidatePendingAsync(int userId, string purpose, CancellationToken cancellationToken = default)
    {
        var pending = await DbSet
            .Where(x => x.UserId == userId && x.Purpose == purpose && !x.IsUsed && !x.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var otp in pending)
        {
            otp.IsDeleted = true;
            otp.UpdatedAt = DateTime.UtcNow;
        }

        if (pending.Count > 0)
        {
            await Context.SaveChangesAsync(cancellationToken);
        }
    }
}
