namespace NOIR.Domain.Events.Product;

/// <summary>
/// Raised when a new product category is created.
/// </summary>
public record ProductCategoryCreatedEvent(
    Guid CategoryId,
    string Name,
    string Slug) : DomainEvent;

/// <summary>
/// Raised when a product category is updated.
/// </summary>
public record ProductCategoryUpdatedEvent(
    Guid CategoryId,
    string Name) : DomainEvent;

/// <summary>
/// Raised when a product category is deleted.
/// </summary>
public record ProductCategoryDeletedEvent(
    Guid CategoryId) : DomainEvent;
