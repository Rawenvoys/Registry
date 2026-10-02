namespace Registry.Infrastructure.Identity.External;

public sealed class ExternalAuthOptions
{
    public const string SectionName = "Authentication:External";

    public OidcProviderOptions Google { get; set; } = new();

    public OidcProviderOptions Microsoft { get; set; } = new();

    public FacebookOptions Facebook { get; set; } = new();
}

public sealed class OidcProviderOptions
{
    /// <summary>Client ids of every app (web, Android, iOS) whose ID tokens the API accepts.</summary>
    public string[] ClientIds { get; set; } = [];

    public bool IsConfigured => ClientIds.Length > 0;
}

public sealed class FacebookOptions
{
    public string? AppId { get; set; }

    public string? AppSecret { get; set; }

    public bool IsConfigured => !string.IsNullOrEmpty(AppId) && !string.IsNullOrEmpty(AppSecret);
}
