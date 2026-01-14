namespace NOIR.Application.Specifications.Notifications;

/// <summary>
/// Specification to get paginated notifications for a user.
/// Supports filtering by read/unread status.
/// </summary>
public sealed class UserNotificationsSpec : Specification<Notification>
{
    public UserNotificationsSpec(string userId, bool includeRead, int page, int pageSize)
    {
        Query.Where(n => n.UserId == userId && !n.IsDeleted);

        if (!includeRead)
        {
            Query.Where(n => !n.IsRead);
        }

        Query.OrderByDescending(n => n.CreatedAt)
             .Skip((page - 1) * pageSize)
             .Take(pageSize)
             .TagWith("UserNotifications");
    }
}
