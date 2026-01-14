namespace NOIR.Application.Specifications.Notifications;

/// <summary>
/// Specification to get a notification by ID for a specific user.
/// Used to ensure users can only access their own notifications.
/// </summary>
public sealed class NotificationByIdSpec : Specification<Notification>
{
    public NotificationByIdSpec(Guid notificationId, string userId)
    {
        Query.Where(n => n.Id == notificationId && n.UserId == userId && !n.IsDeleted)
             .AsTracking()  // Required for entity modification (mark as read)
             .TagWith("NotificationById");
    }
}
