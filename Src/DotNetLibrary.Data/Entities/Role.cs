namespace DotNetLibrary.Data.Entities;

public class Role : BaseEntity
{
    public new int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<UserRoleMap> UserRoleMaps { get; set; } = new List<UserRoleMap>();
    public ICollection<RolePermissionMap> RolePermissionMaps { get; set; } = new List<RolePermissionMap>();
}
