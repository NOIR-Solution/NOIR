namespace NOIR.Application.Features.Auth.Commands.RefreshToken;

/// <summary>
/// Wolverine handler for refreshing access tokens.
/// Implements token rotation with family tracking for theft detection.
/// Supports cookie-based authentication for browser clients.
/// Validation is handled automatically by Wolverine FluentValidation middleware.
/// </summary>
public class RefreshTokenCommandHandler
{
    private readonly IUserIdentityService _userIdentityService;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IDeviceFingerprintService _deviceFingerprintService;
    private readonly ICookieAuthService _cookieAuthService;
    private readonly ILocalizationService _localization;
    private readonly ICurrentUser _currentUser;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IUserIdentityService userIdentityService,
        ITokenService tokenService,
        IRefreshTokenService refreshTokenService,
        IDeviceFingerprintService deviceFingerprintService,
        ICookieAuthService cookieAuthService,
        ILocalizationService localization,
        ICurrentUser currentUser,
        IOptions<JwtSettings> jwtSettings,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _userIdentityService = userIdentityService;
        _tokenService = tokenService;
        _refreshTokenService = refreshTokenService;
        _deviceFingerprintService = deviceFingerprintService;
        _cookieAuthService = cookieAuthService;
        _localization = localization;
        _currentUser = currentUser;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        // Validation is handled by Wolverine FluentValidation middleware

        // Get principal from expired token
        var principal = _tokenService.GetPrincipalFromExpiredToken(command.AccessToken);
        if (principal is null)
        {
            return Result.Failure<AuthResponse>(
                Error.Unauthorized(_localization["auth.token.accessTokenInvalid"], ErrorCodes.Auth.TokenExpired));
        }

        // Get user ID from claims
        var userId = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure<AuthResponse>(
                Error.Unauthorized(_localization["auth.token.accessTokenInvalid"], ErrorCodes.Auth.TokenExpired));
        }

        // Find user
        var user = await _userIdentityService.FindByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<AuthResponse>(
                Error.NotFound(_localization["auth.user.notFound"], ErrorCodes.Auth.UserNotFound));
        }

        // Check if user is active
        if (!user.IsActive)
        {
            return Result.Failure<AuthResponse>(
                Error.Forbidden(_localization["auth.login.accountDisabled"], ErrorCodes.Auth.AccountDisabled));
        }

        // Get refresh token from command or cookie
        var refreshToken = command.RefreshToken ?? _cookieAuthService.GetRefreshTokenFromCookie();
        if (string.IsNullOrEmpty(refreshToken))
        {
            return Result.Failure<AuthResponse>(
                Error.Unauthorized(_localization["auth.token.refreshTokenRequired"], ErrorCodes.Auth.RefreshTokenRequired));
        }

        // Rotate token (validates and creates new token in one operation)
        var newToken = await _refreshTokenService.RotateTokenAsync(
            refreshToken,
            _deviceFingerprintService.GetClientIpAddress(),
            _deviceFingerprintService.GenerateFingerprint(),
            cancellationToken);

        if (newToken is null)
        {
            _logger.LogWarning(
                "Token rotation failed for user {UserId}. Possible token reuse or theft.",
                userId);
            return Result.Failure<AuthResponse>(
                Error.Unauthorized(_localization["auth.token.refreshTokenInvalid"], ErrorCodes.Auth.RefreshTokenInvalid));
        }

        // Generate new access token
        var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email, user.TenantId);
        var accessTokenExpiry = DateTimeOffset.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes);

        // Set cookies if requested (for browser-based auth)
        if (command.UseCookies)
        {
            _cookieAuthService.SetAuthCookies(
                accessToken,
                newToken.Token,
                accessTokenExpiry,
                newToken.ExpiresAt);
        }

        var authResponse = new AuthResponse(
            user.Id,
            user.Email,
            accessToken,
            newToken.Token,
            newToken.ExpiresAt);

        return Result.Success(authResponse);
    }
}
