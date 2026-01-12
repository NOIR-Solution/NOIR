namespace NOIR.Domain.Enums;

/// <summary>
/// Specifies how often email notifications should be sent for a category.
/// </summary>
public enum EmailFrequency
{
    /// <summary>
    /// No email notifications for this category.
    /// </summary>
    None = 0,

    /// <summary>
    /// Send email immediately when notification is triggered.
    /// </summary>
    Immediate = 1,

    /// <summary>
    /// Batch notifications into a daily digest email (sent at 8 AM).
    /// </summary>
    Daily = 2,

    /// <summary>
    /// Batch notifications into a weekly digest email (sent Monday at 8 AM).
    /// </summary>
    Weekly = 3
}
