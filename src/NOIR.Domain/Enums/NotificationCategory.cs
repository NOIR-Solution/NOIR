namespace NOIR.Domain.Enums;

/// <summary>
/// Categorizes notifications by their source/trigger type.
/// Used for filtering and user preference configuration.
/// </summary>
public enum NotificationCategory
{
    /// <summary>
    /// System-level notifications - maintenance, updates, announcements, job failures.
    /// </summary>
    System = 0,

    /// <summary>
    /// User action notifications - mentions, assignments, comments, shares.
    /// </summary>
    UserAction = 1,

    /// <summary>
    /// Workflow notifications - status changes, approvals needed, deadlines.
    /// </summary>
    Workflow = 2,

    /// <summary>
    /// Security notifications - login alerts, password changes, permission updates.
    /// </summary>
    Security = 3,

    /// <summary>
    /// Integration notifications - external API responses, webhook triggers.
    /// </summary>
    Integration = 4
}
