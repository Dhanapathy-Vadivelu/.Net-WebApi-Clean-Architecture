namespace DotNetLibrary.Data.Entities;

public class Permission : BaseEntity
{
    public new int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ICollection<RolePermissionMap> RolePermissionMaps { get; set; } = new List<RolePermissionMap>();
}
