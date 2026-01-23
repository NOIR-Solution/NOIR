namespace NOIR.Application.Features.Notifications.Queries.GetNotifications;


/// <summary>
/// Wolverine handler for getting paginated notifications.
/// </summary>
public class GetNotificationsQueryHandler
{
    private readonly IRepository<Notification, Guid> _repository;
    private readonly ICurrentUser _currentUser;
    private readonly ILocalizationService _localization;

    public GetNotificationsQueryHandler(
        IRepository<Notification, Guid> repository,
        ICurrentUser currentUser,
        ILocalizationService localization)
    {
        _repository = repository;
        _currentUser = currentUser;
        _localization = localization;
    }

    public async Task<Result<NotificationsPagedResponse>> Handle(
        GetNotificationsQuery query,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
        {
            return Result.Failure<NotificationsPagedResponse>(
                Error.Unauthorized(_localization["auth.user.notAuthenticated"], ErrorCodes.Auth.Unauthorized));
        }

        // Platform admins don't receive notifications
        if (_currentUser.IsPlatformAdmin)
        {
            var emptyResponse = new NotificationsPagedResponse(
                Enumerable.Empty<NotificationDto>(),
                0,
                query.Page,
                query.PageSize,
                0);
            return Result.Success(emptyResponse);
        }

        var spec = new UserNotificationsSpec(
            _currentUser.UserId,
            query.IncludeRead,
            query.Page,
            query.PageSize);

        var notifications = await _repository.ListAsync(spec, cancellationToken);
        var totalCount = await _repository.CountAsync(
            new UserNotificationsCountSpec(_currentUser.UserId, query.IncludeRead),
            cancellationToken);

        var items = notifications.Select(MapToDto);
        var totalPages = (int)Math.Ceiling((double)totalCount / query.PageSize);

        var response = new NotificationsPagedResponse(
            items,
            totalCount,
            query.Page,
            query.PageSize,
            totalPages);

        return Result.Success(response);
    }

    private static NotificationDto MapToDto(Notification n) => new(
        n.Id,
        n.Type,
        n.Category,
        n.Title,
        n.Message,
        n.IconClass,
        n.IsRead,
        n.ReadAt,
        n.ActionUrl,
        n.Actions.Select(a => new NotificationActionDto(a.Label, a.Url, a.Style, a.Method)),
        n.CreatedAt);
}
