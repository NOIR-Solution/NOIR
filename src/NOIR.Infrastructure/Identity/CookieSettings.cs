namespace NOIR.Infrastructure.Identity;

/// <summary>
/// Configuration settings for cookie-based authentication.
/// Cookies are used as an alternative transport for JWT tokens,
/// providing seamless authentication for browser-based clients.
/// </summary>
public class CookieSettings
{
    public const string SectionName = "CookieSettings";

    /// <summary>
    /// Name of the cookie storing the JWT access token.
    /// </summary>
    public string AccessTokenCookieName { get; set; } = "noir.access";

    /// <summary>
    /// Name of the cookie storing the refresh token.
    /// </summary>
    public string RefreshTokenCookieName { get; set; } = "noir.refresh";

    /// <summary>
    /// SameSite mode for cookies.
    /// Strict: Cookie only sent in first-party context (most secure, recommended).
    /// Lax: Cookie sent with top-level navigations and GET from third-party sites.
    /// None: Cookie sent in all contexts (requires Secure=true).
    /// </summary>
    [RegularExpression("^(Strict|Lax|None)$", ErrorMessage = "SameSiteMode must be Strict, Lax, or None")]
    public string SameSiteMode { get; set; } = "Strict";

    /// <summary>
    /// Domain for the cookies. Leave empty to use the current domain.
    /// Set to ".example.com" to allow subdomains.
    /// </summary>
    public string? Domain { get; set; }

    /// <summary>
    /// Path for the cookies. Defaults to "/" for site-wide access.
    /// </summary>
    public string Path { get; set; } = "/";

    /// <summary>
    /// Whether cookies should only be sent over HTTPS.
    /// Always true in production. Can be false for local development.
    /// </summary>
    public bool SecureInProduction { get; set; } = true;

    /// <summary>
    /// Gets the SameSiteMode as a SameSiteMode enum value.
    /// </summary>
    public SameSiteMode GetSameSiteMode() => SameSiteMode switch
    {
        "Strict" => Microsoft.AspNetCore.Http.SameSiteMode.Strict,
        "Lax" => Microsoft.AspNetCore.Http.SameSiteMode.Lax,
        "None" => Microsoft.AspNetCore.Http.SameSiteMode.None,
        _ => Microsoft.AspNetCore.Http.SameSiteMode.Strict
    };
}
