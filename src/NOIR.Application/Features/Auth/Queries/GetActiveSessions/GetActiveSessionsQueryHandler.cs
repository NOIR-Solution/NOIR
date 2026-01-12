namespace NOIR.Application.Features.Auth.Queries.GetActiveSessions;

/// <summary>
/// Handler for getting active sessions for the current user.
/// Uses IRefreshTokenService to retrieve session information.
/// </summary>
public class GetActiveSessionsQueryHandler
{
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ICurrentUser _currentUser;
    private readonly ILocalizationService _localization;

    public GetActiveSessionsQueryHandler(
        IRefreshTokenService refreshTokenService,
        ICurrentUser currentUser,
        ILocalizationService localization)
    {
        _refreshTokenService = refreshTokenService;
        _currentUser = currentUser;
        _localization = localization;
    }

    public async Task<Result<IReadOnlyList<ActiveSessionDto>>> Handle(
        GetActiveSessionsQuery query,
        CancellationToken cancellationToken)
    {
        // Check if user is authenticated
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
        {
            return Result.Failure<IReadOnlyList<ActiveSessionDto>>(
                Error.Unauthorized(_localization["auth.user.notAuthenticated"], ErrorCodes.Auth.Unauthorized));
        }

        // Get active sessions
        var sessions = await _refreshTokenService.GetActiveSessionsAsync(
            _currentUser.UserId,
            cancellationToken);

        var sessionDtos = sessions
            .Select(s => new ActiveSessionDto(
                s.Id,
                s.DeviceName,
                s.UserAgent,
                s.CreatedByIp,
                s.CreatedAt,
                s.ExpiresAt,
                IsCurrent: s.Token == query.CurrentRefreshToken))
            .OrderByDescending(s => s.IsCurrent)
            .ThenByDescending(s => s.CreatedAt)
            .ToList();

        return Result.Success<IReadOnlyList<ActiveSessionDto>>(sessionDtos);
    }
}
