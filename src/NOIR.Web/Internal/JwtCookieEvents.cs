namespace NOIR.Web.Internal;

/// <summary>
/// Custom JWT Bearer events that enable reading JWT tokens from cookies.
/// This allows the same JWT authentication to work with both:
/// - Authorization header (for API/SPA clients)
/// - HttpOnly cookies (for browser/SSR clients)
///
/// Priority: Authorization header takes precedence over cookies.
/// </summary>
public class JwtCookieEvents : JwtBearerEvents
{
    private readonly CookieSettings _cookieSettings;

    public JwtCookieEvents(IOptions<CookieSettings> cookieSettings)
    {
        _cookieSettings = cookieSettings.Value;
    }

    /// <summary>
    /// Called when a message is received. If no Authorization header is present,
    /// attempts to read the JWT from the access token cookie.
    /// </summary>
    public override Task MessageReceived(MessageReceivedContext context)
    {
        // Skip if Authorization header is already present (takes precedence)
        if (context.Request.Headers.ContainsKey("Authorization"))
        {
            return base.MessageReceived(context);
        }

        // Try to get token from cookie
        if (context.Request.Cookies.TryGetValue(_cookieSettings.AccessTokenCookieName, out var token))
        {
            if (!string.IsNullOrEmpty(token))
            {
                context.Token = token;
            }
        }

        return base.MessageReceived(context);
    }

    /// <summary>
    /// Called when authentication fails. Clears auth cookies if they contain invalid tokens.
    /// </summary>
    public override Task AuthenticationFailed(AuthenticationFailedContext context)
    {
        // If authentication failed and we have cookies, they might be expired/invalid
        // Clear them to prevent repeated failures
        if (context.Request.Cookies.ContainsKey(_cookieSettings.AccessTokenCookieName))
        {
            // Mark cookies for deletion (actual deletion happens via response)
            context.Response.Cookies.Delete(_cookieSettings.AccessTokenCookieName, new CookieOptions
            {
                Path = _cookieSettings.Path,
                Domain = _cookieSettings.Domain
            });
            context.Response.Cookies.Delete(_cookieSettings.RefreshTokenCookieName, new CookieOptions
            {
                Path = _cookieSettings.Path,
                Domain = _cookieSettings.Domain
            });
        }

        return base.AuthenticationFailed(context);
    }
}
