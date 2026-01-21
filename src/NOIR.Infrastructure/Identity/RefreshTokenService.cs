namespace NOIR.Infrastructure.Identity;

/// <summary>
/// Service for managing refresh tokens with rotation and theft detection.
/// Implements token family tracking to detect and prevent token reuse attacks.
/// Uses Specifications for all database queries per project patterns.
/// </summary>
public class RefreshTokenService : IRefreshTokenService, IScopedService
{
    private readonly IRepository<RefreshToken, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISecureTokenGenerator _tokenGenerator;
    private readonly IOptionsMonitor<JwtSettings> _jwtSettings;
    private readonly ILogger<RefreshTokenService> _logger;

    public RefreshTokenService(
        IRepository<RefreshToken, Guid> repository,
        IUnitOfWork unitOfWork,
        ISecureTokenGenerator tokenGenerator,
        IOptionsMonitor<JwtSettings> jwtSettings,
        ILogger<RefreshTokenService> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _tokenGenerator = tokenGenerator;
        _jwtSettings = jwtSettings;
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
        if (_jwtSettings.CurrentValue.MaxConcurrentSessions > 0)
        {
            var activeCount = await GetActiveSessionCountAsync(userId, cancellationToken);
            if (activeCount >= _jwtSettings.CurrentValue.MaxConcurrentSessions)
            {
                // Revoke oldest session using specification
                var oldestTokenSpec = new OldestActiveRefreshTokenSpec(userId);
                var oldestToken = await _repository.FirstOrDefaultAsync(oldestTokenSpec, cancellationToken);

                if (oldestToken != null)
                {
                    oldestToken.Revoke(ipAddress, "Session limit reached - oldest session revoked");
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation(
                        "Revoked oldest session for user {UserId} due to session limit",
                        userId);
                }
            }
        }

        var token = RefreshToken.Create(
            _tokenGenerator.GenerateToken(64),
            userId,
            _jwtSettings.CurrentValue.RefreshTokenExpirationInDays,
            tenantId,
            ipAddress,
            deviceFingerprint,
            userAgent,
            deviceName);

        await _repository.AddAsync(token, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
        var spec = new RefreshTokenByValueSpec(currentToken);
        var existingToken = await _repository.FirstOrDefaultAsync(spec, cancellationToken);

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
        if (_jwtSettings.CurrentValue.EnableDeviceFingerprinting &&
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
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return null;
        }

        // Create new token with same family
        var newToken = RefreshToken.Create(
            _tokenGenerator.GenerateToken(64),
            existingToken.UserId,
            _jwtSettings.CurrentValue.RefreshTokenExpirationInDays,
            existingToken.TenantId,
            ipAddress,
            deviceFingerprint ?? existingToken.DeviceFingerprint,
            existingToken.UserAgent,
            existingToken.DeviceName,
            existingToken.TokenFamily); // Same family for tracking

        // Revoke old token, link to new
        existingToken.Revoke(ipAddress, "Rotated", newToken.Token);

        await _repository.AddAsync(newToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
        var spec = new RefreshTokenByValueSpec(token);
        var refreshToken = await _repository.FirstOrDefaultAsync(spec, cancellationToken);

        if (refreshToken is null)
        {
            return null;
        }

        if (!refreshToken.IsActive)
        {
            return null;
        }

        // Validate device fingerprint if enabled
        if (_jwtSettings.CurrentValue.EnableDeviceFingerprinting &&
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
        var spec = new RefreshTokenByValueSpec(token);
        var refreshToken = await _repository.FirstOrDefaultAsync(spec, cancellationToken);

        if (refreshToken is null || refreshToken.IsRevoked)
        {
            return;
        }

        refreshToken.Revoke(ipAddress, reason ?? "Manually revoked");
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
        var spec = new ActiveRefreshTokensByUserSpec(userId);
        var tokens = await _repository.ListAsync(spec, cancellationToken);

        foreach (var token in tokens)
        {
            token.Revoke(ipAddress, reason ?? "All sessions revoked");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
        var spec = new RefreshTokensByFamilySpec(tokenFamily);
        var tokens = await _repository.ListAsync(spec, cancellationToken);

        foreach (var token in tokens)
        {
            token.Revoke(ipAddress, reason ?? "Token family revoked");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "Revoked entire token family {TokenFamily} ({Count} tokens). Reason: {Reason}",
            tokenFamily, tokens.Count, reason);
    }

    public async Task<IReadOnlyList<RefreshToken>> GetActiveSessionsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var spec = new ActiveRefreshTokensByUserSpec(userId);
        return await _repository.ListAsync(spec, cancellationToken);
    }

    public async Task CleanupExpiredTokensAsync(
        int daysToKeep = 30,
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTimeOffset.UtcNow.AddDays(-daysToKeep);
        var spec = new ExpiredRefreshTokensSpec(cutoffDate);
        var deletedCount = await _repository.BulkDeleteAsync(spec, cancellationToken);

        _logger.LogInformation(
            "Cleaned up {Count} expired/revoked refresh tokens older than {Days} days",
            deletedCount, daysToKeep);
    }

    public async Task<int> GetActiveSessionCountAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var spec = new ActiveRefreshTokensByUserSpec(userId);
        return await _repository.CountAsync(spec, cancellationToken);
    }
}
