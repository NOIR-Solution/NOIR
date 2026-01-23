namespace NOIR.Application.Features.Notifications.Queries.GetUnreadCount;


/// <summary>
/// Wolverine handler for getting unread notification count.
/// </summary>
public class GetUnreadCountQueryHandler
{
    private readonly IRepository<Notification, Guid> _repository;
    private readonly ICurrentUser _currentUser;
    private readonly ILocalizationService _localization;

    public GetUnreadCountQueryHandler(
        IRepository<Notification, Guid> repository,
        ICurrentUser currentUser,
        ILocalizationService localization)
    {
        _repository = repository;
        _currentUser = currentUser;
        _localization = localization;
    }

    public async Task<Result<UnreadCountResponse>> Handle(
        GetUnreadCountQuery query,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
        {
            return Result.Failure<UnreadCountResponse>(
                Error.Unauthorized(_localization["auth.user.notAuthenticated"], ErrorCodes.Auth.Unauthorized));
        }

        // Platform admins don't receive notifications
        if (_currentUser.IsPlatformAdmin)
        {
            return Result.Success(new UnreadCountResponse(0));
        }

        var spec = new UnreadNotificationsCountSpec(_currentUser.UserId);
        var count = await _repository.CountAsync(spec, cancellationToken);

        return Result.Success(new UnreadCountResponse(count));
    }
}
