namespace NOIR.Application.Features.Notifications.DTOs;

/// <summary>
/// Data transfer object for a notification.
/// </summary>
public sealed record NotificationDto(
    Guid Id,
    NotificationType Type,
    NotificationCategory Category,
    string Title,
    string Message,
    string? IconClass,
    bool IsRead,
    DateTimeOffset? ReadAt,
    string? ActionUrl,
    IEnumerable<NotificationActionDto> Actions,
    DateTimeOffset CreatedAt);

/// <summary>
/// Data transfer object for a notification action button.
/// </summary>
public sealed record NotificationActionDto(
    string Label,
    string Url,
    string? Style,
    string? Method);

/// <summary>
/// Data transfer object for user notification preferences.
/// </summary>
public sealed record NotificationPreferenceDto(
    Guid Id,
    NotificationCategory Category,
    string CategoryName,
    bool InAppEnabled,
    EmailFrequency EmailFrequency);

/// <summary>
/// Paginated response for notifications.
/// </summary>
public sealed record NotificationsPagedResponse(
    IEnumerable<NotificationDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

/// <summary>
/// Response containing unread count.
/// </summary>
public sealed record UnreadCountResponse(int Count);

/// <summary>
/// Request to update notification preferences.
/// </summary>
public sealed record UpdatePreferenceRequest(
    NotificationCategory Category,
    bool InAppEnabled,
    EmailFrequency EmailFrequency);
