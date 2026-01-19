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

    public async Task<Result<LoginResponse>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        // Validation is handled by Wolverine FluentValidation middleware
        var normalizedEmail = _userIdentityService.NormalizeEmail(command.Email);

        // If TenantId is provided (from tenant selection popup), authenticate directly to that tenant
        if (command.TenantId is not null)
        {
            return await AuthenticateToTenant(command, normalizedEmail, command.TenantId, cancellationToken);
        }

        // Find all users with this email across all tenants
        var userTenants = await _userIdentityService.FindTenantsByEmailAsync(normalizedEmail, cancellationToken);

        if (userTenants.Count == 0)
        {
            return Result.Failure<LoginResponse>(
                Error.Unauthorized(_localization["auth.login.invalidCredentials"], ErrorCodes.Auth.InvalidCredentials));
        }

        // Check password for each user and collect matching tenants
        var matchedTenants = new List<(UserTenantInfo TenantInfo, UserIdentityDto User)>();

        foreach (var tenantInfo in userTenants)
        {
            var user = await _userIdentityService.FindByEmailAsync(normalizedEmail, tenantInfo.TenantId, cancellationToken);
            if (user is null || !user.IsActive) continue;

            var result = await _userIdentityService.CheckPasswordSignInAsync(
                user.Id, command.Password, lockoutOnFailure: true, cancellationToken);

            if (result.IsLockedOut)
            {
                return Result.Failure<LoginResponse>(
                    Error.Forbidden(_localization["auth.login.accountLockedOut"], ErrorCodes.Auth.AccountLockedOut));
            }

            if (result.Succeeded)
            {
                matchedTenants.Add((tenantInfo, user));
            }
        }

        if (matchedTenants.Count == 0)
        {
            return Result.Failure<LoginResponse>(
                Error.Unauthorized(_localization["auth.login.invalidCredentials"], ErrorCodes.Auth.InvalidCredentials));
        }

        // Single tenant match - complete login directly
        if (matchedTenants.Count == 1)
        {
            var (_, user) = matchedTenants[0];
            return await CompleteLogin(command, user, cancellationToken);
        }

        // Multiple tenants matched - require tenant selection
        var tenantOptions = matchedTenants
            .Select(m => new TenantOptionDto(m.TenantInfo.TenantId, m.TenantInfo.TenantIdentifier, m.TenantInfo.TenantName))
            .ToList();

        return Result.Success(LoginResponse.SelectTenant(tenantOptions));
    }

    /// <summary>
    /// Authenticate directly to a specific tenant (used after tenant selection).
    /// </summary>
    private async Task<Result<LoginResponse>> AuthenticateToTenant(
        LoginCommand command,
        string normalizedEmail,
        string tenantId,
        CancellationToken cancellationToken)
    {
        var user = await _userIdentityService.FindByEmailAsync(normalizedEmail, tenantId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<LoginResponse>(
                Error.Unauthorized(_localization["auth.login.invalidCredentials"], ErrorCodes.Auth.InvalidCredentials));
        }

        if (!user.IsActive)
        {
            return Result.Failure<LoginResponse>(
                Error.Forbidden(_localization["auth.login.accountDisabled"], ErrorCodes.Auth.AccountDisabled));
        }

        var result = await _userIdentityService.CheckPasswordSignInAsync(
            user.Id, command.Password, lockoutOnFailure: true, cancellationToken);

        if (result.IsLockedOut)
        {
            return Result.Failure<LoginResponse>(
                Error.Forbidden(_localization["auth.login.accountLockedOut"], ErrorCodes.Auth.AccountLockedOut));
        }

        if (!result.Succeeded)
        {
            return Result.Failure<LoginResponse>(
                Error.Unauthorized(_localization["auth.login.invalidCredentials"], ErrorCodes.Auth.InvalidCredentials));
        }

        return await CompleteLogin(command, user, cancellationToken);
    }

    /// <summary>
    /// Complete the login process by generating tokens.
    /// </summary>
    private async Task<Result<LoginResponse>> CompleteLogin(
        LoginCommand command,
        UserIdentityDto user,
        CancellationToken cancellationToken)
    {
        // Generate access token using user's actual TenantId
        var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email, user.TenantId);
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

        // Set cookies if requested
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

        return Result.Success(LoginResponse.Authenticated(authResponse));
    }
}
