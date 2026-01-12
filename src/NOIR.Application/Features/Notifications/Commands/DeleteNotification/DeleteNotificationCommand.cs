namespace NOIR.Application.Features.Notifications.Commands.DeleteNotification;

/// <summary>
/// Command to delete (soft) a notification.
/// </summary>
/// <param name="NotificationId">The ID of the notification to delete.</param>
public sealed record DeleteNotificationCommand(Guid NotificationId);
