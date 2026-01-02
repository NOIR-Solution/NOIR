namespace NOIR.Infrastructure.Identity.Handlers;

/// <summary>
/// Wolverine handler for user registration.
/// Supports both JWT (header) and cookie-based authentication.
/// Validation is handled automatically by Wolverine FluentValidation middleware.
/// </summary>
public class RegisterCommandHandler
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ICookieAuthService _cookieAuthService;
    private readonly JwtSettings _jwtSettings;

    public RegisterCommandHandler(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IRefreshTokenService refreshTokenService,
        ICookieAuthService cookieAuthService,
        IOptions<JwtSettings> jwtSettings)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _refreshTokenService = refreshTokenService;
        _cookieAuthService = cookieAuthService;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<Result<AuthResponse>> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        // Validation is handled by Wolverine FluentValidation middleware

        // Normalize email for consistent storage
        var normalizedEmail = _userManager.NormalizeEmail(command.Email);

        var user = new ApplicationUser
        {
            UserName = command.Email,
            Email = command.Email,
            NormalizedEmail = normalizedEmail,
            FirstName = command.FirstName,
            LastName = command.LastName,
            IsActive = true
            // Note: CreatedAt is set automatically by AuditableEntityInterceptor
        };

        var result = await _userManager.CreateAsync(user, command.Password);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            return Result.Failure<AuthResponse>(Error.ValidationErrors(errors));
        }

        // Assign default "User" role using constant
        await _userManager.AddToRoleAsync(user, Roles.User);

        // Generate access token (minimal JWT - roles/permissions checked on each request)
        var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email!, user.TenantId);
        var accessTokenExpiry = DateTimeOffset.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes);

        // Create refresh token in database (proper token rotation support)
        var refreshToken = await _refreshTokenService.CreateTokenAsync(
            user.Id,
            user.TenantId,
            cancellationToken: cancellationToken);

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
