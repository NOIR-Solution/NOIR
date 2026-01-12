namespace NOIR.Infrastructure.Hubs;

using Microsoft.AspNetCore.SignalR;
using NOIR.Application.Features.Notifications.DTOs;

/// <summary>
/// Implementation of INotificationHubContext using SignalR.
/// Provides abstraction for sending real-time notifications.
/// </summary>
public class NotificationHubContext : INotificationHubContext, IScopedService
{
    private readonly IHubContext<NotificationHub, INotificationClient> _hubContext;
    private readonly ILogger<NotificationHubContext> _logger;

    public NotificationHubContext(
        IHubContext<NotificationHub, INotificationClient> hubContext,
        ILogger<NotificationHubContext> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SendToUserAsync(string userId, NotificationDto notification, CancellationToken ct = default)
    {
        try
        {
            await _hubContext.Clients.Group($"user_{userId}")
                .ReceiveNotification(notification);

            _logger.LogDebug("Sent notification {NotificationId} to user {UserId}",
                notification.Id, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification {NotificationId} to user {UserId}",
                notification.Id, userId);
        }
    }

    /// <inheritdoc />
    public async Task UpdateUnreadCountAsync(string userId, int count, CancellationToken ct = default)
    {
        try
        {
            await _hubContext.Clients.Group($"user_{userId}")
                .UpdateUnreadCount(count);

            _logger.LogDebug("Updated unread count to {Count} for user {UserId}", count, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update unread count for user {UserId}", userId);
        }
    }

    /// <inheritdoc />
    public async Task SendToAllAsync(NotificationDto notification, CancellationToken ct = default)
    {
        try
        {
            await _hubContext.Clients.All.ReceiveNotification(notification);
            _logger.LogDebug("Broadcast notification {NotificationId} to all users", notification.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast notification {NotificationId}", notification.Id);
        }
    }

    /// <inheritdoc />
    public async Task SendToGroupAsync(string groupName, NotificationDto notification, CancellationToken ct = default)
    {
        try
        {
            await _hubContext.Clients.Group(groupName).ReceiveNotification(notification);
            _logger.LogDebug("Sent notification {NotificationId} to group {GroupName}",
                notification.Id, groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification {NotificationId} to group {GroupName}",
                notification.Id, groupName);
        }
    }
}
