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

    /// <summary>
    /// Notifies clients that the server is shutting down gracefully.
    /// </summary>
    Task ReceiveServerShutdown(string reason);

    /// <summary>
    /// Notifies clients that the server has recovered from a restart.
    /// </summary>
    Task ReceiveServerRecovery();

    /// <summary>
    /// Notifies clients subscribed to a specific entity instance that it was updated.
    /// </summary>
    Task EntityUpdated(EntityUpdateSignal signal);

    /// <summary>
    /// Notifies clients subscribed to an entity list that the collection changed.
    /// </summary>
    Task EntityCollectionUpdated(EntityUpdateSignal signal);
}
