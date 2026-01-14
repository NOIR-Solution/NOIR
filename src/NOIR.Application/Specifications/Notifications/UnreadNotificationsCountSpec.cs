namespace NOIR.Application.Specifications.Notifications;

/// <summary>
/// Specification to count unread notifications for a user.
/// Used to update the unread badge count in the UI via SignalR.
/// </summary>
public sealed class UnreadNotificationsCountSpec : Specification<Notification>
{
    public UnreadNotificationsCountSpec(string userId)
    {
        Query.Where(n => n.UserId == userId && !n.IsRead && !n.IsDeleted)
             .TagWith("UnreadNotificationsCount");
    }
}
