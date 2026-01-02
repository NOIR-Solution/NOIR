namespace NOIR.Infrastructure.Identity;

/// <summary>
/// Service for managing authentication cookies.
/// Implements secure cookie handling with HttpOnly, Secure, and SameSite flags.
/// </summary>
public class CookieAuthService : ICookieAuthService, IScopedService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly CookieSettings _cookieSettings;
    private readonly IHostEnvironment _environment;

    public CookieAuthService(
        IHttpContextAccessor httpContextAccessor,
        IOptions<CookieSettings> cookieSettings,
        IHostEnvironment environment)
    {
        _httpContextAccessor = httpContextAccessor;
        _cookieSettings = cookieSettings.Value;
        _environment = environment;
    }

    public void SetAuthCookies(
        string accessToken,
        string refreshToken,
        DateTimeOffset accessTokenExpiry,
        DateTimeOffset refreshTokenExpiry)
    {
        var httpContext = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext is not available");

        // Secure flag: Always true in production, configurable in development
        var isProduction = !_environment.IsDevelopment();
        var secure = isProduction ? _cookieSettings.SecureInProduction : false;

        // Set access token cookie
        httpContext.Response.Cookies.Append(
            _cookieSettings.AccessTokenCookieName,
            accessToken,
            CreateCookieOptions(accessTokenExpiry, secure));

        // Set refresh token cookie
        httpContext.Response.Cookies.Append(
            _cookieSettings.RefreshTokenCookieName,
            refreshToken,
            CreateCookieOptions(refreshTokenExpiry, secure));
    }

    public void ClearAuthCookies()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null) return;

        var deleteOptions = new CookieOptions
        {
            Path = _cookieSettings.Path,
            Domain = _cookieSettings.Domain,
            HttpOnly = true,
            SameSite = _cookieSettings.GetSameSiteMode()
        };

        httpContext.Response.Cookies.Delete(_cookieSettings.AccessTokenCookieName, deleteOptions);
        httpContext.Response.Cookies.Delete(_cookieSettings.RefreshTokenCookieName, deleteOptions);
    }

    public string? GetRefreshTokenFromCookie()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null) return null;

        return httpContext.Request.Cookies.TryGetValue(_cookieSettings.RefreshTokenCookieName, out var token)
            ? token
            : null;
    }

    public string? GetAccessTokenFromCookie()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null) return null;

        return httpContext.Request.Cookies.TryGetValue(_cookieSettings.AccessTokenCookieName, out var token)
            ? token
            : null;
    }

    private CookieOptions CreateCookieOptions(DateTimeOffset expires, bool secure) => new()
    {
        Expires = expires,
        HttpOnly = true,
        Secure = secure,
        SameSite = _cookieSettings.GetSameSiteMode(),
        Path = _cookieSettings.Path,
        Domain = _cookieSettings.Domain,
        IsEssential = true // Auth cookies are essential
    };
}
