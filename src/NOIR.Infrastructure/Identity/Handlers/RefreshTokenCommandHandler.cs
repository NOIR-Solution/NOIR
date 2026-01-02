namespace NOIR.Infrastructure.Identity.Handlers;

/// <summary>
/// Wolverine handler for refreshing access tokens.
/// Implements token rotation with family tracking for theft detection.
/// Validation is handled automatically by Wolverine FluentValidation middleware.
/// </summary>
public class RefreshTokenCommandHandler
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IDeviceFingerprintService _deviceFingerprintService;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IRefreshTokenService refreshTokenService,
        IDeviceFingerprintService deviceFingerprintService,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _refreshTokenService = refreshTokenService;
        _deviceFingerprintService = deviceFingerprintService;
        _logger = logger;
    }

    public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        // Validation is handled by Wolverine FluentValidation middleware

        // Get principal from expired token
        var principal = _tokenService.GetPrincipalFromExpiredToken(command.AccessToken);
        if (principal is null)
        {
            return Result.Failure<AuthResponse>(Error.Unauthorized("Invalid access token."));
        }

        // Get user ID from claims
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure<AuthResponse>(Error.Unauthorized("Invalid access token."));
        }

        // Find user
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Result.Failure<AuthResponse>(Error.NotFound("User not found."));
        }

        // Check if user is active
        if (!user.IsActive)
        {
            return Result.Failure<AuthResponse>(Error.Forbidden("User account is disabled."));
        }

        // Rotate token (validates and creates new token in one operation)
        var newToken = await _refreshTokenService.RotateTokenAsync(
            command.RefreshToken,
            _deviceFingerprintService.GetClientIpAddress(),
            _deviceFingerprintService.GenerateFingerprint(),
            cancellationToken);

        if (newToken is null)
        {
            _logger.LogWarning(
                "Token rotation failed for user {UserId}. Possible token reuse or theft.",
                userId);
            return Result.Failure<AuthResponse>(Error.Unauthorized("Invalid or expired refresh token."));
        }

        // Generate new access token
        var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email!, user.TenantId);

        var authResponse = new AuthResponse(
            user.Id,
            user.Email!,
            accessToken,
            newToken.Token,
            newToken.ExpiresAt);

        return Result.Success(authResponse);
    }
}
