namespace DotNetLibrary.Data.Entities;

public class User : BaseEntity
{
    public new int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? EmailVerifiedAt { get; set; }
    public ICollection<UserRoleMap> UserRoleMaps { get; set; } = new List<UserRoleMap>();
    public ICollection<UserOtp> UserOtps { get; set; } = new List<UserOtp>();
}
