namespace MGAO.Core;

/// <summary>
/// Embedded OAuth credentials for GWSMO-parity user experience.
/// Users don't need to configure Google Cloud - just install and sign in.
///
/// Environment variables MGAO_CLIENT_ID and MGAO_CLIENT_SECRET can override
/// these for development or custom deployments.
/// </summary>
public static class AppCredentials
{
    // Embedded credentials for MGAO Google Cloud project (mgao-484309)
    // Project status: Testing (up to 100 test users)
    private const string EmbeddedClientId = "28776681828-e5gf087pr1koqbk1m5hfk2gannl4pnl7.apps.googleusercontent.com";
    private const string EmbeddedClientSecret = "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";

    /// <summary>
    /// Gets the OAuth Client ID. Environment variable takes precedence if set.
    /// </summary>
    public static string ClientId =>
        Environment.GetEnvironmentVariable("MGAO_CLIENT_ID") is { Length: > 0 } envId
            ? envId
            : EmbeddedClientId;

    /// <summary>
    /// Gets the OAuth Client Secret. Environment variable takes precedence if set.
    /// </summary>
    public static string ClientSecret =>
        Environment.GetEnvironmentVariable("MGAO_CLIENT_SECRET") is { Length: > 0 } envSecret
            ? envSecret
            : EmbeddedClientSecret;

    /// <summary>
    /// Returns true if using embedded credentials (not overridden by env vars).
    /// </summary>
    public static bool IsUsingEmbeddedCredentials =>
        string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MGAO_CLIENT_ID"));
}
