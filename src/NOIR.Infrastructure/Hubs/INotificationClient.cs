namespace NOIR.Infrastructure.Hubs;

using NOIR.Application.Features.Notifications.DTOs;

/// <summary>
/// Strongly-typed SignalR client interface for notifications.
/// Defines methods that can be called on connected clients.
/// </summary>
public interface INotificationClient
{
    /// <summary>
    /// Receives a new notification.
    /// </summary>
    Task ReceiveNotification(NotificationDto notification);

    /// <summary>
    /// Updates the unread notification count.
    /// </summary>
    Task UpdateUnreadCount(int count);
}
