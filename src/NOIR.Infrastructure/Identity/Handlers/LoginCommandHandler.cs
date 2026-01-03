namespace NOIR.Infrastructure.Identity.Handlers;

/// <summary>
/// Wolverine handler for user login.
/// Uses the RefreshToken entity with family tracking and device fingerprinting.
/// Supports both JWT (header) and cookie-based authentication.
/// Validation is handled automatically by Wolverine FluentValidation middleware.
/// </summary>
public class LoginCommandHandler
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IDeviceFingerprintService _deviceFingerprintService;
    private readonly ICookieAuthService _cookieAuthService;
    private readonly ILocalizationService _localization;
    private readonly JwtSettings _jwtSettings;

    public LoginCommandHandler(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService,
        IRefreshTokenService refreshTokenService,
        IDeviceFingerprintService deviceFingerprintService,
        ICookieAuthService cookieAuthService,
        ILocalizationService localization,
        IOptions<JwtSettings> jwtSettings)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _refreshTokenService = refreshTokenService;
        _deviceFingerprintService = deviceFingerprintService;
        _cookieAuthService = cookieAuthService;
        _localization = localization;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<Result<AuthResponse>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        // Validation is handled by Wolverine FluentValidation middleware

        // Normalize email for consistent lookup
        var normalizedEmail = _userManager.NormalizeEmail(command.Email);
        var user = await _userManager.FindByEmailAsync(normalizedEmail);

        if (user is null)
        {
            return Result.Failure<AuthResponse>(Error.Unauthorized(_localization["auth.login.invalidCredentials"]));
        }

        if (!user.IsActive)
        {
            return Result.Failure<AuthResponse>(Error.Forbidden(_localization["auth.login.accountDisabled"]));
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, command.Password, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            return Result.Failure<AuthResponse>(Error.Forbidden(_localization["auth.login.accountLockedOut"]));
        }

        if (!result.Succeeded)
        {
            return Result.Failure<AuthResponse>(Error.Unauthorized(_localization["auth.login.invalidCredentials"]));
        }

        // Generate access token
        var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email!, user.TenantId);
        var accessTokenExpiry = DateTimeOffset.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes);

        // Create refresh token with device tracking
        var refreshToken = await _refreshTokenService.CreateTokenAsync(
            user.Id,
            user.TenantId,
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
            user.Email!,
            accessToken,
            refreshToken.Token,
            refreshToken.ExpiresAt);

        return Result.Success(authResponse);
    }
}
