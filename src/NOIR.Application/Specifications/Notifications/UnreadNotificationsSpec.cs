namespace NOIR.Application.Specifications.Notifications;

/// <summary>
/// Specification to get all unread notifications for a user.
/// Used for batch operations like marking all as read.
/// </summary>
public sealed class UnreadNotificationsSpec : Specification<Notification>
{
    public UnreadNotificationsSpec(string userId)
    {
        Query.Where(n => n.UserId == userId && !n.IsRead && !n.IsDeleted)
             .AsTracking()  // Required for entity modification (mark as read)
             .TagWith("UnreadNotifications");
    }
}
