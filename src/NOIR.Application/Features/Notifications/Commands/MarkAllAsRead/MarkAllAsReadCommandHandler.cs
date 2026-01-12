namespace NOIR.Application.Features.Notifications.Commands.MarkAllAsRead;

using NOIR.Domain.Interfaces;

/// <summary>
/// Wolverine handler for marking all notifications as read.
/// </summary>
public class MarkAllAsReadCommandHandler
{
    private readonly IRepository<Notification, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationHubContext _hubContext;
    private readonly ILocalizationService _localization;

    public MarkAllAsReadCommandHandler(
        IRepository<Notification, Guid> repository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        INotificationHubContext hubContext,
        ILocalizationService localization)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _hubContext = hubContext;
        _localization = localization;
    }

    public async Task<Result<int>> Handle(MarkAllAsReadCommand command, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
        {
            return Result.Failure<int>(
                Error.Unauthorized(_localization["auth.user.notAuthenticated"], ErrorCodes.Auth.Unauthorized));
        }

        var spec = new UnreadNotificationsSpec(_currentUser.UserId);
        var unreadNotifications = await _repository.ListAsync(spec, cancellationToken);

        var count = 0;
        foreach (var notification in unreadNotifications)
        {
            notification.MarkAsRead();
            count++;
        }

        if (count > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Update unread count to 0 via SignalR
            await _hubContext.UpdateUnreadCountAsync(_currentUser.UserId, 0, cancellationToken);
        }

        return Result.Success(count);
    }
}
