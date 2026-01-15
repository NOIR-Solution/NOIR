namespace NOIR.Domain.Entities;

/// <summary>
/// Email template entity for storing customizable email templates.
/// Supports template variables for admin portal editing.
/// </summary>
public class EmailTemplate : TenantAggregateRoot<Guid>
{
    /// <summary>
    /// Unique template identifier/name (e.g., "PasswordResetOtp", "WelcomeEmail").
    /// Used to look up templates programmatically.
    /// </summary>
    public string Name { get; private set; } = default!;

    /// <summary>
    /// Email subject line. Supports template variables like {{UserName}}, {{OtpCode}}.
    /// </summary>
    public string Subject { get; private set; } = default!;

    /// <summary>
    /// HTML body content. Supports template variables.
    /// </summary>
    public string HtmlBody { get; private set; } = default!;

    /// <summary>
    /// Plain text body for email clients that don't support HTML.
    /// </summary>
    public string? PlainTextBody { get; private set; }

    /// <summary>
    /// Whether this template is active and should be used.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Template version for tracking changes.
    /// Incremented each time the template is updated.
    /// </summary>
    public int Version { get; private set; }

    /// <summary>
    /// Description of the template purpose for admin reference.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Available variables that can be used in this template.
    /// Stored as JSON array for reference (e.g., ["UserName", "OtpCode", "ExpiryMinutes"]).
    /// </summary>
    public string? AvailableVariables { get; private set; }

    // Private constructor for EF Core
    private EmailTemplate() : base() { }

    /// <summary>
    /// Creates a new email template.
    /// </summary>
    public static EmailTemplate Create(
        string name,
        string subject,
        string htmlBody,
        string? plainTextBody = null,
        string? description = null,
        string? availableVariables = null,
        string? tenantId = null)
    {
        return new EmailTemplate
        {
            Id = Guid.NewGuid(),
            Name = name,
            Subject = subject,
            HtmlBody = htmlBody,
            PlainTextBody = plainTextBody,
            IsActive = true,
            Version = 1,
            Description = description,
            AvailableVariables = availableVariables,
            TenantId = tenantId
        };
    }

    /// <summary>
    /// Updates the template content and increments version.
    /// </summary>
    public void Update(
        string subject,
        string htmlBody,
        string? plainTextBody = null,
        string? description = null,
        string? availableVariables = null)
    {
        Subject = subject;
        HtmlBody = htmlBody;
        PlainTextBody = plainTextBody;
        Description = description;
        AvailableVariables = availableVariables;
        Version++;
    }

    /// <summary>
    /// Activates the template.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// Deactivates the template.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }
}
