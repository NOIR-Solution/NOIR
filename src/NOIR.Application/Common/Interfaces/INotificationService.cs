namespace NOIR.Application.Common.Interfaces;

using NOIR.Application.Features.Notifications.DTOs;
using NOIR.Domain.Enums;

/// <summary>
/// Service for sending notifications to users.
/// Implements the "Persist then Notify" pattern - saves to database first,
/// then delivers via SignalR for real-time updates.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Sends a notification to a specific user.
    /// </summary>
    Task<Result<NotificationDto>> SendToUserAsync(
        string userId,
        NotificationType type,
        NotificationCategory category,
        string title,
        string message,
        string? iconClass = null,
        string? actionUrl = null,
        IEnumerable<NotificationActionDto>? actions = null,
        string? metadata = null,
        CancellationToken ct = default);

    /// <summary>
    /// Sends a notification to all users with a specific role.
    /// </summary>
    Task<Result<int>> SendToRoleAsync(
        string roleName,
        NotificationType type,
        NotificationCategory category,
        string title,
        string message,
        string? iconClass = null,
        string? actionUrl = null,
        IEnumerable<NotificationActionDto>? actions = null,
        string? metadata = null,
        CancellationToken ct = default);

    /// <summary>
    /// Broadcasts a notification to all users.
    /// </summary>
    Task<Result<int>> BroadcastAsync(
        NotificationType type,
        NotificationCategory category,
        string title,
        string message,
        string? iconClass = null,
        string? actionUrl = null,
        IEnumerable<NotificationActionDto>? actions = null,
        string? metadata = null,
        CancellationToken ct = default);
}
