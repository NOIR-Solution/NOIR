namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Token pair containing access and refresh tokens.
/// </summary>
public sealed record TokenPair(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt);

/// <summary>
/// Service for generating and validating JWT tokens.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates an access token with minimal claims (userId, email, tenantId).
    /// Roles and permissions are NOT stored in JWT - they're queried from database on each request.
    /// This ensures real-time authorization updates without requiring re-login.
    /// </summary>
    string GenerateAccessToken(string userId, string email, string? tenantId = null);

    /// <summary>
    /// Generates a cryptographically secure refresh token.
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Generates both access and refresh tokens as a pair.
    /// </summary>
    TokenPair GenerateTokenPair(string userId, string email, string? tenantId = null);

    /// <summary>
    /// Gets the expiration time for a new refresh token.
    /// </summary>
    DateTimeOffset GetRefreshTokenExpiry();

    /// <summary>
    /// Validates that the refresh token has a valid format.
    /// Note: Actual token verification (matching stored token, expiry) is done in the handler.
    /// </summary>
    bool IsRefreshTokenFormatValid(string token);

    /// <summary>
    /// Extracts claims from an expired access token for token refresh scenarios.
    /// </summary>
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
