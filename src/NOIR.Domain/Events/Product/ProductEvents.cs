namespace NOIR.Domain.Events.Product;

/// <summary>
/// Raised when a new product is created.
/// </summary>
public record ProductCreatedEvent(
    Guid ProductId,
    string Name,
    string Slug) : DomainEvent;

/// <summary>
/// Raised when a product is published.
/// </summary>
public record ProductPublishedEvent(
    Guid ProductId,
    string Name) : DomainEvent;

/// <summary>
/// Raised when a product is archived.
/// </summary>
public record ProductArchivedEvent(
    Guid ProductId) : DomainEvent;

/// <summary>
/// Raised when a product's stock changes.
/// </summary>
public record ProductStockChangedEvent(
    Guid ProductVariantId,
    Guid ProductId,
    int OldQuantity,
    int NewQuantity,
    InventoryMovementType MovementType) : DomainEvent;
