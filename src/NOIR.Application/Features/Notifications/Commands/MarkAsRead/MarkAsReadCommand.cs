namespace NOIR.Application.Features.Notifications.Commands.MarkAsRead;

/// <summary>
/// Command to mark a specific notification as read.
/// </summary>
/// <param name="NotificationId">The ID of the notification to mark as read.</param>
public sealed record MarkAsReadCommand(Guid NotificationId);
