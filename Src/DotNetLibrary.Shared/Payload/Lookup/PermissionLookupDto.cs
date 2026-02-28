namespace DotNetLibrary.Shared.Payload.Lookup;

public record PermissionLookupDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}
