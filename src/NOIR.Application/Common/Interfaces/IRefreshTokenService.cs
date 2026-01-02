namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Service for managing refresh tokens with rotation and theft detection.
/// </summary>
public interface IRefreshTokenService
{
    /// <summary>
    /// Creates a new refresh token for a user.
    /// </summary>
    Task<RefreshToken> CreateTokenAsync(
        string userId,
        string? tenantId = null,
        string? ipAddress = null,
        string? deviceFingerprint = null,
        string? userAgent = null,
        string? deviceName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rotates a refresh token, creating a new one and revoking the old.
    /// Implements token family tracking for theft detection.
    /// </summary>
    Task<RefreshToken?> RotateTokenAsync(
        string currentToken,
        string? ipAddress = null,
        string? deviceFingerprint = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a refresh token and returns it if valid.
    /// </summary>
    Task<RefreshToken?> ValidateTokenAsync(
        string token,
        string? deviceFingerprint = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a specific refresh token.
    /// </summary>
    Task RevokeTokenAsync(
        string token,
        string? ipAddress = null,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all refresh tokens for a user.
    /// </summary>
    Task RevokeAllUserTokensAsync(
        string userId,
        string? ipAddress = null,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes an entire token family (used when theft is detected).
    /// </summary>
    Task RevokeTokenFamilyAsync(
        Guid tokenFamily,
        string? ipAddress = null,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active sessions (tokens) for a user.
    /// </summary>
    Task<IReadOnlyList<RefreshToken>> GetActiveSessionsAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up expired and old revoked tokens.
    /// </summary>
    Task CleanupExpiredTokensAsync(
        int daysToKeep = 30,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of active sessions for a user.
    /// </summary>
    Task<int> GetActiveSessionCountAsync(
        string userId,
        CancellationToken cancellationToken = default);
}
