namespace NOIR.Infrastructure.Identity;

/// <summary>
/// Service for managing refresh tokens with rotation and theft detection.
/// Implements token family tracking to detect and prevent token reuse attacks.
/// </summary>
public class RefreshTokenService : IRefreshTokenService, IScopedService
{
    private readonly ApplicationDbContext _context;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<RefreshTokenService> _logger;

    public RefreshTokenService(
        ApplicationDbContext context,
        IOptions<JwtSettings> jwtSettings,
        ILogger<RefreshTokenService> logger)
    {
        _context = context;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    public async Task<RefreshToken> CreateTokenAsync(
        string userId,
        string? tenantId = null,
        string? ipAddress = null,
        string? deviceFingerprint = null,
        string? userAgent = null,
        string? deviceName = null,
        CancellationToken cancellationToken = default)
    {
        // Check max concurrent sessions
        if (_jwtSettings.MaxConcurrentSessions > 0)
        {
            var activeCount = await GetActiveSessionCountAsync(userId, cancellationToken);
            if (activeCount >= _jwtSettings.MaxConcurrentSessions)
            {
                // Revoke oldest session
                var oldestToken = await _context.RefreshTokens
                    .Where(t => t.UserId == userId)
                    .Where(t => !t.RevokedAt.HasValue)
                    .Where(t => t.ExpiresAt > DateTimeOffset.UtcNow)
                    .OrderBy(t => t.CreatedAt)
                    .FirstOrDefaultAsync(cancellationToken);

                if (oldestToken != null)
                {
                    oldestToken.Revoke(ipAddress, "Session limit reached - oldest session revoked");
                    _logger.LogInformation(
                        "Revoked oldest session for user {UserId} due to session limit",
                        userId);
                }
            }
        }

        var token = RefreshToken.Create(
            userId,
            _jwtSettings.RefreshTokenExpirationInDays,
            tenantId,
            ipAddress,
            deviceFingerprint,
            userAgent,
            deviceName);

        _context.RefreshTokens.Add(token);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug(
            "Created refresh token for user {UserId}, family {TokenFamily}",
            userId, token.TokenFamily);

        return token;
    }

    public async Task<RefreshToken?> RotateTokenAsync(
        string currentToken,
        string? ipAddress = null,
        string? deviceFingerprint = null,
        CancellationToken cancellationToken = default)
    {
        var existingToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == currentToken, cancellationToken);

        if (existingToken is null)
        {
            _logger.LogWarning("Rotation attempted for non-existent token");
            return null;
        }

        // Check if token is active
        if (!existingToken.IsActive)
        {
            // Token was already used - possible token theft!
            if (existingToken.IsRevoked && existingToken.ReplacedByToken != null)
            {
                _logger.LogWarning(
                    "Refresh token reuse detected for user {UserId}. " +
                    "Revoking entire token family {TokenFamily}",
                    existingToken.UserId, existingToken.TokenFamily);

                // Revoke entire token family - this is a security incident
                await RevokeTokenFamilyAsync(
                    existingToken.TokenFamily,
                    ipAddress,
                    "Token reuse detected - possible theft",
                    cancellationToken);
            }

            return null;
        }

        // Validate device fingerprint if enabled
        if (_jwtSettings.EnableDeviceFingerprinting &&
            !string.IsNullOrEmpty(existingToken.DeviceFingerprint) &&
            existingToken.DeviceFingerprint != deviceFingerprint)
        {
            _logger.LogWarning(
                "Device fingerprint mismatch for user {UserId}. " +
                "Expected: {Expected}, Got: {Actual}",
                existingToken.UserId,
                existingToken.DeviceFingerprint,
                deviceFingerprint);

            existingToken.Revoke(ipAddress, "Device fingerprint mismatch");
            await _context.SaveChangesAsync(cancellationToken);

            return null;
        }

        // Create new token with same family
        var newToken = RefreshToken.Create(
            existingToken.UserId,
            _jwtSettings.RefreshTokenExpirationInDays,
            existingToken.TenantId,
            ipAddress,
            deviceFingerprint ?? existingToken.DeviceFingerprint,
            existingToken.UserAgent,
            existingToken.DeviceName,
            existingToken.TokenFamily); // Same family for tracking

        // Revoke old token, link to new
        existingToken.Revoke(ipAddress, "Rotated", newToken.Token);

        _context.RefreshTokens.Add(newToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug(
            "Rotated refresh token for user {UserId}, family {TokenFamily}",
            existingToken.UserId, existingToken.TokenFamily);

        return newToken;
    }

    public async Task<RefreshToken?> ValidateTokenAsync(
        string token,
        string? deviceFingerprint = null,
        CancellationToken cancellationToken = default)
    {
        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == token, cancellationToken);

        if (refreshToken is null)
        {
            return null;
        }

        if (!refreshToken.IsActive)
        {
            return null;
        }

        // Validate device fingerprint if enabled
        if (_jwtSettings.EnableDeviceFingerprinting &&
            !string.IsNullOrEmpty(refreshToken.DeviceFingerprint) &&
            refreshToken.DeviceFingerprint != deviceFingerprint)
        {
            _logger.LogWarning(
                "Device fingerprint validation failed for user {UserId}",
                refreshToken.UserId);
            return null;
        }

        return refreshToken;
    }

    public async Task RevokeTokenAsync(
        string token,
        string? ipAddress = null,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == token, cancellationToken);

        if (refreshToken is null || refreshToken.IsRevoked)
        {
            return;
        }

        refreshToken.Revoke(ipAddress, reason ?? "Manually revoked");
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Revoked refresh token for user {UserId}",
            refreshToken.UserId);
    }

    public async Task RevokeAllUserTokensAsync(
        string userId,
        string? ipAddress = null,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var tokens = await _context.RefreshTokens
            .Where(t => t.UserId == userId)
            .Where(t => !t.RevokedAt.HasValue)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.Revoke(ipAddress, reason ?? "All sessions revoked");
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Revoked all {Count} refresh tokens for user {UserId}",
            tokens.Count, userId);
    }

    public async Task RevokeTokenFamilyAsync(
        Guid tokenFamily,
        string? ipAddress = null,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var tokens = await _context.RefreshTokens
            .Where(t => t.TokenFamily == tokenFamily)
            .Where(t => !t.RevokedAt.HasValue)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.Revoke(ipAddress, reason ?? "Token family revoked");
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "Revoked entire token family {TokenFamily} ({Count} tokens). Reason: {Reason}",
            tokenFamily, tokens.Count, reason);
    }

    public async Task<IReadOnlyList<RefreshToken>> GetActiveSessionsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens
            .Where(t => t.UserId == userId)
            .Where(t => !t.RevokedAt.HasValue)
            .Where(t => t.ExpiresAt > DateTimeOffset.UtcNow)
            .OrderByDescending(t => t.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task CleanupExpiredTokensAsync(
        int daysToKeep = 30,
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTimeOffset.UtcNow.AddDays(-daysToKeep);

        var deletedCount = await _context.RefreshTokens
            .IgnoreQueryFilters()
            .Where(t => t.ExpiresAt < DateTimeOffset.UtcNow || t.RevokedAt.HasValue)
            .Where(t => t.CreatedAt < cutoffDate)
            .ExecuteDeleteAsync(cancellationToken);

        _logger.LogInformation(
            "Cleaned up {Count} expired/revoked refresh tokens older than {Days} days",
            deletedCount, daysToKeep);
    }

    public async Task<int> GetActiveSessionCountAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens
            .Where(t => t.UserId == userId)
            .Where(t => !t.RevokedAt.HasValue)
            .Where(t => t.ExpiresAt > DateTimeOffset.UtcNow)
            .CountAsync(cancellationToken);
    }
}
