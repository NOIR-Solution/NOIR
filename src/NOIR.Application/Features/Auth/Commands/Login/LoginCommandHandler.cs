namespace NOIR.Application.Features.Auth.Commands.Login;

/// <summary>
/// Wolverine handler for user login.
/// Uses the RefreshToken entity with family tracking and device fingerprinting.
/// Supports both JWT (header) and cookie-based authentication.
/// Validation is handled automatically by Wolverine FluentValidation middleware.
/// </summary>
public class LoginCommandHandler
{
    private readonly IUserIdentityService _userIdentityService;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IDeviceFingerprintService _deviceFingerprintService;
    private readonly ICookieAuthService _cookieAuthService;
    private readonly ILocalizationService _localization;
    private readonly ICurrentUser _currentUser;
    private readonly JwtSettings _jwtSettings;

    public LoginCommandHandler(
        IUserIdentityService userIdentityService,
        ITokenService tokenService,
        IRefreshTokenService refreshTokenService,
        IDeviceFingerprintService deviceFingerprintService,
        ICookieAuthService cookieAuthService,
        ILocalizationService localization,
        ICurrentUser currentUser,
        IOptions<JwtSettings> jwtSettings)
    {
        _userIdentityService = userIdentityService;
        _tokenService = tokenService;
        _refreshTokenService = refreshTokenService;
        _deviceFingerprintService = deviceFingerprintService;
        _cookieAuthService = cookieAuthService;
        _localization = localization;
        _currentUser = currentUser;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<Result<AuthResponse>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        // Validation is handled by Wolverine FluentValidation middleware

        // Normalize email for consistent lookup
        var normalizedEmail = _userIdentityService.NormalizeEmail(command.Email);
        var user = await _userIdentityService.FindByEmailAsync(normalizedEmail, cancellationToken);

        if (user is null)
        {
            return Result.Failure<AuthResponse>(
                Error.Unauthorized(_localization["auth.login.invalidCredentials"], ErrorCodes.Auth.InvalidCredentials));
        }

        if (!user.IsActive)
        {
            return Result.Failure<AuthResponse>(
                Error.Forbidden(_localization["auth.login.accountDisabled"], ErrorCodes.Auth.AccountDisabled));
        }

        var result = await _userIdentityService.CheckPasswordSignInAsync(
            user.Id, command.Password, lockoutOnFailure: true, cancellationToken);

        if (result.IsLockedOut)
        {
            return Result.Failure<AuthResponse>(
                Error.Forbidden(_localization["auth.login.accountLockedOut"], ErrorCodes.Auth.AccountLockedOut));
        }

        if (!result.Succeeded)
        {
            return Result.Failure<AuthResponse>(
                Error.Unauthorized(_localization["auth.login.invalidCredentials"], ErrorCodes.Auth.InvalidCredentials));
        }

        // Generate access token (tenant context comes from request)
        var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email, _currentUser.TenantId);
        var accessTokenExpiry = DateTimeOffset.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes);

        // Create refresh token with device tracking
        var refreshToken = await _refreshTokenService.CreateTokenAsync(
            user.Id,
            _currentUser.TenantId,
            _deviceFingerprintService.GetClientIpAddress(),
            _deviceFingerprintService.GenerateFingerprint(),
            _deviceFingerprintService.GetUserAgent(),
            _deviceFingerprintService.GetDeviceName(),
            cancellationToken);

        // Set cookies if requested (for browser-based auth)
        if (command.UseCookies)
        {
            _cookieAuthService.SetAuthCookies(
                accessToken,
                refreshToken.Token,
                accessTokenExpiry,
                refreshToken.ExpiresAt);
        }

        var authResponse = new AuthResponse(
            user.Id,
            user.Email,
            accessToken,
            refreshToken.Token,
            refreshToken.ExpiresAt);

        return Result.Success(authResponse);
    }
}
