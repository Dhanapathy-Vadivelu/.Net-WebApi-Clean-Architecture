namespace DotNetLibrary.Shared.Payload.Lookup;

public record RoleLookupDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}
