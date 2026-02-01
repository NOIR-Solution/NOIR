namespace NOIR.Domain.Events.Notification;

/// <summary>
/// Raised when a notification is created and persisted.
/// </summary>
public sealed record NotificationCreatedEvent(
    Guid NotificationId,
    string UserId,
    NotificationType Type,
    NotificationCategory Category,
    string Title) : DomainEvent;

/// <summary>
/// Raised when a notification is marked as read.
/// </summary>
public sealed record NotificationMarkedAsReadEvent(
    Guid NotificationId,
    string UserId) : DomainEvent;

/// <summary>
/// Raised when a notification is marked as unread.
/// </summary>
public sealed record NotificationMarkedAsUnreadEvent(
    Guid NotificationId,
    string UserId) : DomainEvent;

/// <summary>
/// Raised when an email notification is sent.
/// </summary>
public sealed record NotificationEmailSentEvent(
    Guid NotificationId,
    string UserId) : DomainEvent;
