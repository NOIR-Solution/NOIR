namespace NOIR.Application.Specifications.Notifications;

/// <summary>
/// Specification to get all notification preferences for a user.
/// Returns preferences for all notification categories.
/// </summary>
public sealed class UserPreferencesSpec : Specification<NotificationPreference>
{
    public UserPreferencesSpec(string userId, bool asTracking = false)
    {
        Query.Where(p => p.UserId == userId)
             .OrderBy(p => p.Category)
             .TagWith("UserPreferences");

        if (asTracking)
        {
            Query.AsTracking();
        }
    }
}
