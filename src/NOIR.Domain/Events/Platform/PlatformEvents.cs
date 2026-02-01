namespace NOIR.Domain.Events.Platform;

/// <summary>
/// Raised when an email template is created.
/// </summary>
public sealed record EmailTemplateCreatedEvent(
    Guid TemplateId,
    string TemplateKey,
    string? TenantId) : DomainEvent;

/// <summary>
/// Raised when an email template is updated.
/// </summary>
public sealed record EmailTemplateUpdatedEvent(
    Guid TemplateId,
    string TemplateKey,
    int NewVersion) : DomainEvent;

/// <summary>
/// Raised when an email template is activated.
/// </summary>
public sealed record EmailTemplateActivatedEvent(
    Guid TemplateId,
    string TemplateKey) : DomainEvent;

/// <summary>
/// Raised when an email template is deactivated.
/// </summary>
public sealed record EmailTemplateDeactivatedEvent(
    Guid TemplateId,
    string TemplateKey) : DomainEvent;

/// <summary>
/// Raised when a legal page is created.
/// </summary>
public sealed record LegalPageCreatedEvent(
    Guid PageId,
    string PageType,
    string? TenantId) : DomainEvent;

/// <summary>
/// Raised when a legal page is updated.
/// </summary>
public sealed record LegalPageUpdatedEvent(
    Guid PageId,
    string PageType,
    int NewVersion) : DomainEvent;

/// <summary>
/// Raised when a legal page is activated.
/// </summary>
public sealed record LegalPageActivatedEvent(
    Guid PageId,
    string PageType) : DomainEvent;

/// <summary>
/// Raised when a legal page is deactivated.
/// </summary>
public sealed record LegalPageDeactivatedEvent(
    Guid PageId,
    string PageType) : DomainEvent;
