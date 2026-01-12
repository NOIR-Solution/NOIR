namespace NOIR.Application.Features.Auth.Commands.RevokeSession;

/// <summary>
/// Handler for revoking a specific session by ID.
/// Ensures users can only revoke their own sessions.
/// </summary>
public class RevokeSessionCommandHandler
{
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ICurrentUser _currentUser;
    private readonly ILocalizationService _localization;

    public RevokeSessionCommandHandler(
        IRefreshTokenService refreshTokenService,
        ICurrentUser currentUser,
        ILocalizationService localization)
    {
        _refreshTokenService = refreshTokenService;
        _currentUser = currentUser;
        _localization = localization;
    }

    public async Task<Result> Handle(RevokeSessionCommand command, CancellationToken cancellationToken)
    {
        // Check if user is authenticated
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
        {
            return Result.Failure(
                Error.Unauthorized(_localization["auth.user.notAuthenticated"], ErrorCodes.Auth.Unauthorized));
        }

        // Get user's active sessions
        var sessions = await _refreshTokenService.GetActiveSessionsAsync(
            _currentUser.UserId,
            cancellationToken);

        // Find the session by ID
        var session = sessions.FirstOrDefault(s => s.Id == command.SessionId);
        if (session is null)
        {
            return Result.Failure(
                Error.NotFound(_localization["auth.session.notFound"], "Session.NotFound"));
        }

        // Revoke the session
        await _refreshTokenService.RevokeTokenAsync(
            session.Token,
            command.IpAddress,
            "User requested session revocation",
            cancellationToken);

        return Result.Success();
    }
}
