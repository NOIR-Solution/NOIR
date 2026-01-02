namespace NOIR.Infrastructure.Identity.Handlers;

/// <summary>
/// Wolverine handler for user logout.
/// Clears authentication cookies and revokes refresh tokens.
/// </summary>
public class LogoutCommandHandler
{
    private readonly ICookieAuthService _cookieAuthService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ICurrentUser _currentUser;

    public LogoutCommandHandler(
        ICookieAuthService cookieAuthService,
        IRefreshTokenService refreshTokenService,
        ICurrentUser currentUser)
    {
        _cookieAuthService = cookieAuthService;
        _refreshTokenService = refreshTokenService;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        // Always clear cookies (even if not authenticated, to clean up stale cookies)
        _cookieAuthService.ClearAuthCookies();

        // If not authenticated, just return success (cookies cleared)
        if (string.IsNullOrEmpty(_currentUser.UserId))
        {
            return Result.Success();
        }

        // Determine which refresh token to revoke
        var refreshToken = command.RefreshToken ?? _cookieAuthService.GetRefreshTokenFromCookie();

        if (command.RevokeAllSessions)
        {
            // Revoke all refresh tokens for this user
            await _refreshTokenService.RevokeAllUserTokensAsync(
                _currentUser.UserId,
                reason: "User logout (all sessions)",
                cancellationToken: cancellationToken);
        }
        else if (!string.IsNullOrEmpty(refreshToken))
        {
            // Revoke only the current refresh token
            await _refreshTokenService.RevokeTokenAsync(
                refreshToken,
                reason: "User logout",
                cancellationToken: cancellationToken);
        }

        return Result.Success();
    }
}
