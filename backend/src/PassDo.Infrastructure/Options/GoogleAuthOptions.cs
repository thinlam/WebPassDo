namespace PassDo.Infrastructure.Options;

public class GoogleAuthOptions
{
    public const string SectionName = "Google";

    /// <summary>OAuth 2.0 Client ID (public, safe to expose to the SPA). No client secret is stored server-side.</summary>
    public string ClientId { get; set; } = string.Empty;
}
