namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Service for managing authentication cookies.
/// Provides methods to set and clear JWT tokens in HttpOnly cookies.
/// </summary>
public interface ICookieAuthService
{
    /// <summary>
    /// Sets authentication cookies containing the JWT access token and refresh token.
    /// Cookies are configured with HttpOnly, Secure, and SameSite flags for security.
    /// </summary>
    /// <param name="accessToken">The JWT access token.</param>
    /// <param name="refreshToken">The refresh token.</param>
    /// <param name="accessTokenExpiry">Expiration time for the access token cookie.</param>
    /// <param name="refreshTokenExpiry">Expiration time for the refresh token cookie.</param>
    void SetAuthCookies(string accessToken, string refreshToken, DateTimeOffset accessTokenExpiry, DateTimeOffset refreshTokenExpiry);

    /// <summary>
    /// Clears the authentication cookies from the response.
    /// Used during logout or when tokens are invalidated.
    /// </summary>
    void ClearAuthCookies();

    /// <summary>
    /// Gets the refresh token from the cookie if present.
    /// </summary>
    /// <returns>The refresh token or null if not present.</returns>
    string? GetRefreshTokenFromCookie();

    /// <summary>
    /// Gets the access token from the cookie if present.
    /// </summary>
    /// <returns>The access token or null if not present.</returns>
    string? GetAccessTokenFromCookie();
}
