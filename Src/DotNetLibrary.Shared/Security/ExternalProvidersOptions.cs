namespace DotNetLibrary.Shared.Security;

public class ExternalProvidersOptions
{
    public GoogleOptions Google { get; set; } = new();
    public MicrosoftEntraOptions Microsoft { get; set; } = new();
}

public class GoogleOptions
{
    public string ClientId { get; set; } = string.Empty;
    public bool RequireHttpsMetadata { get; set; } = true;
}

public class MicrosoftEntraOptions
{
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public bool RequireHttpsMetadata { get; set; } = true;
}
