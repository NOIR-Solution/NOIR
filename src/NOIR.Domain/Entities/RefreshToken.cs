namespace NOIR.Domain.Entities;

/// <summary>
/// Refresh token entity for secure token rotation and theft detection.
/// Implements token family tracking to detect and prevent token reuse attacks.
/// Extends TenantAggregateRoot for consistent audit tracking and domain events.
/// </summary>
public class RefreshToken : TenantAggregateRoot<Guid>
{
    /// <summary>
    /// The actual token value (cryptographically secure random string).
    /// </summary>
    public string Token { get; private set; } = default!;

    /// <summary>
    /// The user this token belongs to.
    /// </summary>
    public string UserId { get; private set; } = default!;

    /// <summary>
    /// When this token expires.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>
    /// IP address that created this token.
    /// </summary>
    public string? CreatedByIp { get; private set; }

    /// <summary>
    /// When this token was revoked (if applicable).
    /// </summary>
    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>
    /// IP address that revoked this token.
    /// </summary>
    public string? RevokedByIp { get; private set; }

    /// <summary>
    /// The token that replaced this one (for rotation tracking).
    /// </summary>
    public string? ReplacedByToken { get; private set; }

    /// <summary>
    /// Reason for revocation.
    /// </summary>
    public string? ReasonRevoked { get; private set; }

    /// <summary>
    /// Device fingerprint for binding token to device.
    /// </summary>
    public string? DeviceFingerprint { get; private set; }

    /// <summary>
    /// User agent string for device identification.
    /// </summary>
    public string? UserAgent { get; private set; }

    /// <summary>
    /// Device name for user-friendly display.
    /// </summary>
    public string? DeviceName { get; private set; }

    /// <summary>
    /// Token family ID for detecting token reuse/theft.
    /// All tokens in a rotation chain share the same family ID.
    /// </summary>
    public Guid TokenFamily { get; private set; }

    /// <summary>
    /// Whether this token has expired.
    /// </summary>
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;

    /// <summary>
    /// Whether this token has been revoked.
    /// </summary>
    public bool IsRevoked => RevokedAt.HasValue;

    /// <summary>
    /// Whether this token is still active (not expired and not revoked).
    /// </summary>
    public bool IsActive => !IsRevoked && !IsExpired;

    // Private constructor for EF Core
    private RefreshToken() : base() { }

    /// <summary>
    /// Creates a new refresh token.
    /// </summary>
    /// <param name="token">Pre-generated cryptographically secure token value.</param>
    /// <param name="userId">The user this token belongs to.</param>
    /// <param name="expirationDays">Token validity duration in days.</param>
    /// <param name="tenantId">Tenant ID for multi-tenancy.</param>
    /// <param name="ipAddress">IP address that created this token.</param>
    /// <param name="deviceFingerprint">Device fingerprint for binding.</param>
    /// <param name="userAgent">User agent string.</param>
    /// <param name="deviceName">Device name for display.</param>
    /// <param name="tokenFamily">Token family for rotation tracking.</param>
    public static RefreshToken Create(
        string token,
        string userId,
        int expirationDays,
        string? tenantId = null,
        string? ipAddress = null,
        string? deviceFingerprint = null,
        string? userAgent = null,
        string? deviceName = null,
        Guid? tokenFamily = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = token,
            UserId = userId,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(expirationDays),
            CreatedByIp = ipAddress,
            DeviceFingerprint = deviceFingerprint,
            UserAgent = userAgent,
            DeviceName = deviceName,
            TokenFamily = tokenFamily ?? Guid.NewGuid(),
            TenantId = tenantId
        };

        refreshToken.AddDomainEvent(new Events.Auth.RefreshTokenCreatedEvent(
            refreshToken.Id,
            userId,
            deviceName ?? userAgent));

        return refreshToken;
    }

    /// <summary>
    /// Revokes this token.
    /// </summary>
    public void Revoke(string? ipAddress = null, string? reason = null, string? replacedByToken = null)
    {
        RevokedAt = DateTimeOffset.UtcNow;
        RevokedByIp = ipAddress;
        ReasonRevoked = reason;
        ReplacedByToken = replacedByToken;

        AddDomainEvent(new Events.Auth.RefreshTokenRevokedEvent(Id, UserId, reason));
    }
}
