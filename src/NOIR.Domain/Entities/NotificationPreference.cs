namespace NOIR.Domain.Entities;

using NOIR.Domain.Enums;

/// <summary>
/// Stores a user's notification preferences for a specific category.
/// Controls in-app notification display and email frequency.
/// </summary>
public class NotificationPreference : TenantAggregateRoot<Guid>
{
    /// <summary>
    /// The user ID these preferences belong to.
    /// </summary>
    public string UserId { get; private set; } = default!;

    /// <summary>
    /// The notification category these preferences apply to.
    /// </summary>
    public NotificationCategory Category { get; private set; }

    /// <summary>
    /// Whether to show in-app notifications for this category.
    /// </summary>
    public bool InAppEnabled { get; private set; }

    /// <summary>
    /// Email delivery frequency for this category.
    /// </summary>
    public EmailFrequency EmailFrequency { get; private set; }

    // Private constructor for EF Core
    private NotificationPreference() : base() { }

    /// <summary>
    /// Creates a new notification preference with default settings.
    /// Defaults: In-app enabled, Daily email digest.
    /// </summary>
    public static NotificationPreference Create(
        string userId,
        NotificationCategory category,
        bool inAppEnabled = true,
        EmailFrequency emailFrequency = EmailFrequency.Daily,
        string? tenantId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        return new NotificationPreference
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Category = category,
            InAppEnabled = inAppEnabled,
            EmailFrequency = emailFrequency,
            TenantId = tenantId
        };
    }

    /// <summary>
    /// Creates default preferences for all categories for a new user.
    /// </summary>
    public static IEnumerable<NotificationPreference> CreateDefaults(
        string userId,
        string? tenantId = null)
    {
        foreach (NotificationCategory category in Enum.GetValues<NotificationCategory>())
        {
            // Security notifications get immediate email by default
            var emailFreq = category == NotificationCategory.Security
                ? EmailFrequency.Immediate
                : EmailFrequency.Daily;

            yield return Create(userId, category, true, emailFreq, tenantId);
        }
    }

    /// <summary>
    /// Updates the preference settings.
    /// </summary>
    public void Update(bool inAppEnabled, EmailFrequency emailFrequency)
    {
        InAppEnabled = inAppEnabled;
        EmailFrequency = emailFrequency;
    }

    /// <summary>
    /// Enables in-app notifications for this category.
    /// </summary>
    public void EnableInApp()
    {
        InAppEnabled = true;
    }

    /// <summary>
    /// Disables in-app notifications for this category.
    /// </summary>
    public void DisableInApp()
    {
        InAppEnabled = false;
    }

    /// <summary>
    /// Sets the email frequency for this category.
    /// </summary>
    public void SetEmailFrequency(EmailFrequency frequency)
    {
        EmailFrequency = frequency;
    }
}
