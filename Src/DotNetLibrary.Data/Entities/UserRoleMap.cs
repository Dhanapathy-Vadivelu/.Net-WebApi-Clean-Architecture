namespace DotNetLibrary.Data.Entities;

public class UserRoleMap : BaseEntity
{
    public new int Id { get; set; }
    public int UserId { get; set; }
    public int RoleId { get; set; }

    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
