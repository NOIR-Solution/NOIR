namespace NOIR.Application.Common.Interfaces;

using NOIR.Application.Features.Notifications.DTOs;

/// <summary>
/// Abstraction for SignalR hub context to enable real-time notification delivery.
/// Implemented in Infrastructure layer using IHubContext&lt;NotificationHub, INotificationClient&gt;.
/// </summary>
public interface INotificationHubContext
{
    /// <summary>
    /// Sends a notification to a specific user via SignalR.
    /// </summary>
    Task SendToUserAsync(string userId, NotificationDto notification, CancellationToken ct = default);

    /// <summary>
    /// Updates the unread count for a specific user.
    /// </summary>
    Task UpdateUnreadCountAsync(string userId, int count, CancellationToken ct = default);

    /// <summary>
    /// Sends a notification to all connected clients.
    /// </summary>
    Task SendToAllAsync(NotificationDto notification, CancellationToken ct = default);

    /// <summary>
    /// Sends a notification to all users in a specific group (e.g., role-based).
    /// </summary>
    Task SendToGroupAsync(string groupName, NotificationDto notification, CancellationToken ct = default);
}
