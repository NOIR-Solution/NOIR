namespace NOIR.Application.Specifications.Notifications;

/// <summary>
/// Specification to find a user's notification preferences for a specific category.
/// Used to check whether in-app notifications are enabled and email frequency settings.
/// </summary>
public sealed class UserPreferencesByCategorySpec : Specification<NotificationPreference>
{
    public UserPreferencesByCategorySpec(string userId, NotificationCategory category)
    {
        Query.Where(p => p.UserId == userId && p.Category == category)
             .AsTracking()  // Required for entity modification (update preferences)
             .TagWith("UserPreferencesByCategory");
    }
}
