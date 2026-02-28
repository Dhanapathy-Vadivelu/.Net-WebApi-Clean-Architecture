namespace DotNetLibrary.Data.Entities;

public class UserOtp : BaseEntity
{
    public int UserId { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public string CodeHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public bool IsUsed { get; set; }

    public User User { get; set; } = null!;
}
