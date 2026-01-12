namespace NOIR.Domain.ValueObjects;

/// <summary>
/// Represents an actionable button/link within a notification.
/// Supports multi-action notifications (e.g., Approve/Reject/View).
/// </summary>
public class NotificationAction : ValueObject
{
    /// <summary>
    /// Display text for the action button.
    /// </summary>
    public string Label { get; private set; } = default!;

    /// <summary>
    /// URL to navigate to or API endpoint to call.
    /// </summary>
    public string Url { get; private set; } = default!;

    /// <summary>
    /// Visual style of the button: "primary", "secondary", or "destructive".
    /// </summary>
    public string? Style { get; private set; }

    /// <summary>
    /// HTTP method for API actions: "GET", "POST", "PUT", "DELETE".
    /// If null, defaults to navigation (GET).
    /// </summary>
    public string? Method { get; private set; }

    // Private constructor for EF Core
    private NotificationAction() { }

    /// <summary>
    /// Creates a new notification action.
    /// </summary>
    public static NotificationAction Create(
        string label,
        string url,
        string? style = null,
        string? method = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        return new NotificationAction
        {
            Label = label,
            Url = url,
            Style = style,
            Method = method
        };
    }

    /// <summary>
    /// Creates a primary action button (blue/default).
    /// </summary>
    public static NotificationAction Primary(string label, string url, string? method = null)
        => Create(label, url, "primary", method);

    /// <summary>
    /// Creates a secondary action button (outline/gray).
    /// </summary>
    public static NotificationAction Secondary(string label, string url, string? method = null)
        => Create(label, url, "secondary", method);

    /// <summary>
    /// Creates a destructive action button (red).
    /// </summary>
    public static NotificationAction Destructive(string label, string url, string? method = null)
        => Create(label, url, "destructive", method);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Label;
        yield return Url;
        yield return Style;
        yield return Method;
    }
}
