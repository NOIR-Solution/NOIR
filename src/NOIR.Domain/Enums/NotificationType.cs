namespace NOIR.Domain.Enums;

/// <summary>
/// Represents the visual type/severity of a notification.
/// Used for color-coding and icon selection in the UI.
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// Informational notification (blue) - general updates, tips.
    /// </summary>
    Info = 0,

    /// <summary>
    /// Success notification (green) - completed actions, achievements.
    /// </summary>
    Success = 1,

    /// <summary>
    /// Warning notification (amber) - potential issues, approaching deadlines.
    /// </summary>
    Warning = 2,

    /// <summary>
    /// Error notification (red) - failures, critical issues requiring attention.
    /// </summary>
    Error = 3
}
