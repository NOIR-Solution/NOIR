namespace NOIR.Domain.Entities;

using NOIR.Domain.Enums;
using NOIR.Domain.ValueObjects;

/// <summary>
/// Represents an in-app notification for a user.
/// Supports real-time delivery via SignalR and multi-action buttons.
/// </summary>
public class Notification : TenantAggregateRoot<Guid>
{
    /// <summary>
    /// The user ID this notification belongs to.
    /// </summary>
    public string UserId { get; private set; } = default!;

    /// <summary>
    /// Visual type/severity of the notification (Info, Success, Warning, Error).
    /// </summary>
    public NotificationType Type { get; private set; }

    /// <summary>
    /// Category for filtering and preference matching.
    /// </summary>
    public NotificationCategory Category { get; private set; }

    /// <summary>
    /// Short title displayed prominently.
    /// </summary>
    public string Title { get; private set; } = default!;

    /// <summary>
    /// Detailed message body.
    /// </summary>
    public string Message { get; private set; } = default!;

    /// <summary>
    /// Optional icon class (e.g., Lucide icon name).
    /// If null, a default icon based on Type is used.
    /// </summary>
    public string? IconClass { get; private set; }

    /// <summary>
    /// Whether the notification has been read by the user.
    /// </summary>
    public bool IsRead { get; private set; }

    /// <summary>
    /// Timestamp when the notification was marked as read.
    /// </summary>
    public DateTimeOffset? ReadAt { get; private set; }

    /// <summary>
    /// Primary URL to navigate when clicking the notification.
    /// </summary>
    public string? ActionUrl { get; private set; }

    /// <summary>
    /// Collection of action buttons for the notification.
    /// Stored as JSON in the database.
    /// </summary>
    private readonly List<NotificationAction> _actions = [];
    public IReadOnlyCollection<NotificationAction> Actions => _actions.AsReadOnly();

    /// <summary>
    /// Additional metadata stored as JSON.
    /// Can contain entity references, context data, etc.
    /// </summary>
    public string? Metadata { get; private set; }

    /// <summary>
    /// Whether email was sent for this notification.
    /// Used to track immediate email delivery.
    /// </summary>
    public bool EmailSent { get; private set; }

    /// <summary>
    /// Whether this notification was included in a digest email.
    /// Used to prevent duplicate digest entries.
    /// </summary>
    public bool IncludedInDigest { get; private set; }

    // Private constructor for EF Core
    private Notification() : base() { }

    /// <summary>
    /// Creates a new notification.
    /// </summary>
    public static Notification Create(
        string userId,
        NotificationType type,
        NotificationCategory category,
        string title,
        string message,
        string? iconClass = null,
        string? actionUrl = null,
        string? metadata = null,
        string? tenantId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Category = category,
            Title = title,
            Message = message,
            IconClass = iconClass,
            ActionUrl = actionUrl,
            Metadata = metadata,
            IsRead = false,
            EmailSent = false,
            IncludedInDigest = false,
            TenantId = tenantId
        };

        notification.AddDomainEvent(new Events.Notification.NotificationCreatedEvent(
            notification.Id,
            userId,
            type,
            category,
            title));

        return notification;
    }

    /// <summary>
    /// Adds an action button to the notification.
    /// </summary>
    public Notification AddAction(NotificationAction action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _actions.Add(action);
        return this;
    }

    /// <summary>
    /// Adds multiple action buttons.
    /// </summary>
    public Notification AddActions(IEnumerable<NotificationAction> actions)
    {
        ArgumentNullException.ThrowIfNull(actions);
        _actions.AddRange(actions);
        return this;
    }

    /// <summary>
    /// Marks the notification as read.
    /// </summary>
    public void MarkAsRead()
    {
        if (!IsRead)
        {
            IsRead = true;
            ReadAt = DateTimeOffset.UtcNow;

            AddDomainEvent(new Events.Notification.NotificationMarkedAsReadEvent(Id, UserId));
        }
    }

    /// <summary>
    /// Marks the notification as unread.
    /// </summary>
    public void MarkAsUnread()
    {
        if (IsRead)
        {
            IsRead = false;
            ReadAt = null;

            AddDomainEvent(new Events.Notification.NotificationMarkedAsUnreadEvent(Id, UserId));
        }
    }

    /// <summary>
    /// Records that an email was sent for this notification.
    /// </summary>
    public void MarkEmailSent()
    {
        if (!EmailSent)
        {
            EmailSent = true;

            AddDomainEvent(new Events.Notification.NotificationEmailSentEvent(Id, UserId));
        }
    }

    /// <summary>
    /// Records that this notification was included in a digest email.
    /// </summary>
    public void MarkIncludedInDigest()
    {
        IncludedInDigest = true;
    }
}
