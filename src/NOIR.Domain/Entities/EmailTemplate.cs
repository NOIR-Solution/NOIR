namespace NOIR.Domain.Entities;

/// <summary>
/// Email template entity for storing customizable email templates.
/// Platform-level entity - NOT scoped to any tenant by default.
/// When TenantId is null, this is a platform-level default template.
/// When TenantId has a value, this is a tenant-specific override (copy-on-edit).
/// Supports template variables for admin portal editing.
/// </summary>
public class EmailTemplate : PlatformTenantAggregateRoot<Guid>, ISeedableEntity
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

    // Note: TenantId, IsPlatformDefault, IsTenantOverride, and IAuditableEntity properties
    // are inherited from PlatformTenantAggregateRoot<Guid> base class

    // Private constructor for EF Core
    private EmailTemplate() : base() { }

    /// <summary>
    /// Creates a platform-level default template (TenantId = null).
    /// Platform templates are shared across all tenants and serve as defaults.
    /// </summary>
    public static EmailTemplate CreatePlatformDefault(
        string name,
        string subject,
        string htmlBody,
        string? plainTextBody = null,
        string? description = null,
        string? availableVariables = null)
    {
        var template = new EmailTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = null, // Platform default
            Name = name,
            Subject = subject,
            HtmlBody = htmlBody,
            PlainTextBody = plainTextBody,
            IsActive = true,
            Version = 1,
            Description = description,
            AvailableVariables = availableVariables
        };

        template.AddDomainEvent(new Events.Platform.EmailTemplateCreatedEvent(
            template.Id,
            name,
            null));

        return template;
    }

    /// <summary>
    /// Creates a tenant-specific template override.
    /// Used when a tenant customizes a platform template (copy-on-edit pattern).
    /// </summary>
    public static EmailTemplate CreateTenantOverride(
        string tenantId,
        string name,
        string subject,
        string htmlBody,
        string? plainTextBody = null,
        string? description = null,
        string? availableVariables = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId, nameof(tenantId));

        var template = new EmailTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Subject = subject,
            HtmlBody = htmlBody,
            PlainTextBody = plainTextBody,
            IsActive = true,
            Version = 1,
            Description = description,
            AvailableVariables = availableVariables
        };

        template.AddDomainEvent(new Events.Platform.EmailTemplateCreatedEvent(
            template.Id,
            name,
            tenantId));

        return template;
    }

    /// <summary>
    /// Creates a new email template (legacy method for backward compatibility).
    /// Use CreatePlatformDefault() or CreateTenantOverride() for clearer semantics.
    /// </summary>
    [Obsolete("Use CreatePlatformDefault() or CreateTenantOverride() for clearer semantics.")]
    public static EmailTemplate Create(
        string name,
        string subject,
        string htmlBody,
        string? plainTextBody = null,
        string? description = null,
        string? availableVariables = null,
        string? tenantId = null)
    {
        return tenantId == null
            ? CreatePlatformDefault(name, subject, htmlBody, plainTextBody, description, availableVariables)
            : CreateTenantOverride(tenantId, name, subject, htmlBody, plainTextBody, description, availableVariables);
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

        AddDomainEvent(new Events.Platform.EmailTemplateUpdatedEvent(Id, Name, Version));
    }

    /// <summary>
    /// Activates the template.
    /// </summary>
    public void Activate()
    {
        if (!IsActive)
        {
            IsActive = true;
            AddDomainEvent(new Events.Platform.EmailTemplateActivatedEvent(Id, Name));
        }
    }

    /// <summary>
    /// Deactivates the template.
    /// </summary>
    public void Deactivate()
    {
        if (IsActive)
        {
            IsActive = false;
            AddDomainEvent(new Events.Platform.EmailTemplateDeactivatedEvent(Id, Name));
        }
    }

    /// <summary>
    /// Resets version to 1. FOR SEEDING USE ONLY.
    /// This is used when platform defaults are updated during application startup.
    /// Prevents the need for reflection-based version manipulation.
    /// Should NOT be called from business logic - only from database seeders.
    /// </summary>
    public void ResetVersionForSeeding()
    {
        Version = 1;
    }
}
