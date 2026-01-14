namespace NOIR.Application.Specifications.Notifications;

/// <summary>
/// Specification to count notifications for a user.
/// Used for pagination total count calculation.
/// </summary>
public sealed class UserNotificationsCountSpec : Specification<Notification>
{
    public UserNotificationsCountSpec(string userId, bool includeRead)
    {
        Query.Where(n => n.UserId == userId && !n.IsDeleted);

        if (!includeRead)
        {
            Query.Where(n => !n.IsRead);
        }

        Query.TagWith("UserNotificationsCount");
    }
}
