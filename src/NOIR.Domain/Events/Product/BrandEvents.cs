namespace NOIR.Domain.Events.Product;

/// <summary>
/// Raised when a new brand is created.
/// </summary>
public record BrandCreatedEvent(
    Guid BrandId,
    string Name,
    string Slug) : DomainEvent;

/// <summary>
/// Raised when a brand is updated.
/// </summary>
public record BrandUpdatedEvent(
    Guid BrandId,
    string Name) : DomainEvent;

/// <summary>
/// Raised when a brand is deleted.
/// </summary>
public record BrandDeletedEvent(
    Guid BrandId) : DomainEvent;
