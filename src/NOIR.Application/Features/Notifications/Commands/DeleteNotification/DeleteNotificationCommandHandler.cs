namespace NOIR.Application.Features.Notifications.Commands.DeleteNotification;

using NOIR.Domain.Interfaces;

/// <summary>
/// Wolverine handler for deleting a notification (soft delete).
/// </summary>
public class DeleteNotificationCommandHandler
{
    private readonly IRepository<Notification, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationHubContext _hubContext;
    private readonly ILocalizationService _localization;

    public DeleteNotificationCommandHandler(
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

    public async Task<Result> Handle(DeleteNotificationCommand command, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
        {
            return Result.Failure(
                Error.Unauthorized(_localization["auth.user.notAuthenticated"], ErrorCodes.Auth.Unauthorized));
        }

        var spec = new NotificationByIdSpec(command.NotificationId, _currentUser.UserId);
        var notification = await _repository.FirstOrDefaultAsync(spec, cancellationToken);

        if (notification is null)
        {
            return Result.Failure(
                Error.NotFound(_localization["notifications.notFound"], "NOTIFICATION_NOT_FOUND"));
        }

        // Soft delete via repository (handled by interceptor)
        _repository.Remove(notification);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Update unread count if the deleted notification was unread
        if (!notification.IsRead)
        {
            var unreadCount = await _repository.CountAsync(
                new UnreadNotificationsCountSpec(_currentUser.UserId),
                cancellationToken);
            await _hubContext.UpdateUnreadCountAsync(_currentUser.UserId, unreadCount, cancellationToken);
        }

        return Result.Success();
    }
}
