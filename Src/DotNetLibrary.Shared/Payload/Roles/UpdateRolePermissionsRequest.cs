using System.ComponentModel.DataAnnotations;

namespace DotNetLibrary.Shared.Payload.Roles;

public class UpdateRolePermissionsRequest
{
    [Required]
    public IEnumerable<int> PermissionIds { get; set; } = Array.Empty<int>();
}
