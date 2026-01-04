namespace NOIR.Application.Features.Auth.Commands.Register;

/// <summary>
/// Wolverine handler for user registration.
/// Supports both JWT (header) and cookie-based authentication.
/// Validation is handled automatically by Wolverine FluentValidation middleware.
/// </summary>
public class RegisterCommandHandler
{
    private readonly IUserIdentityService _userIdentityService;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ICookieAuthService _cookieAuthService;
    private readonly JwtSettings _jwtSettings;

    public RegisterCommandHandler(
        IUserIdentityService userIdentityService,
        ITokenService tokenService,
        IRefreshTokenService refreshTokenService,
        ICookieAuthService cookieAuthService,
        IOptions<JwtSettings> jwtSettings)
    {
        _userIdentityService = userIdentityService;
        _tokenService = tokenService;
        _refreshTokenService = refreshTokenService;
        _cookieAuthService = cookieAuthService;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<Result<AuthResponse>> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        // Validation is handled by Wolverine FluentValidation middleware

        var createUserDto = new CreateUserDto(
            command.Email,
            command.FirstName,
            command.LastName,
            null, // DisplayName
            null  // TenantId
        );

        var createResult = await _userIdentityService.CreateUserAsync(createUserDto, command.Password, cancellationToken);

        if (!createResult.Succeeded)
        {
            return Result.Failure<AuthResponse>(
                Error.ValidationErrors(createResult.Errors!, ErrorCodes.Validation.General));
        }

        var userId = createResult.UserId!;

        // Assign default "User" role using constant
        await _userIdentityService.AddToRolesAsync(userId, [NOIR.Domain.Common.Roles.User], cancellationToken);

        // Get the newly created user
        var user = await _userIdentityService.FindByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<AuthResponse>(
                Error.Failure(ErrorCodes.System.UnknownError, "Failed to retrieve created user"));
        }

        // Generate access token (minimal JWT - roles/permissions checked on each request)
        var accessToken = _tokenService.GenerateAccessToken(userId, user.Email, user.TenantId);
        var accessTokenExpiry = DateTimeOffset.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes);

        // Create refresh token in database (proper token rotation support)
        var refreshToken = await _refreshTokenService.CreateTokenAsync(
            userId,
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
            userId,
            user.Email,
            accessToken,
            refreshToken.Token,
            refreshToken.ExpiresAt);

        return Result.Success(authResponse);
    }
}
