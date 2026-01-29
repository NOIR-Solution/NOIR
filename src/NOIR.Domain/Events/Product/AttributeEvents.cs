namespace NOIR.Domain.Events.Product;

/// <summary>
/// Raised when a new product attribute is created.
/// </summary>
public record ProductAttributeCreatedEvent(
    Guid AttributeId,
    string Code,
    string Name,
    AttributeType Type) : DomainEvent;

/// <summary>
/// Raised when a product attribute is updated.
/// </summary>
public record ProductAttributeUpdatedEvent(
    Guid AttributeId,
    string Code,
    string Name) : DomainEvent;

/// <summary>
/// Raised when a product attribute is deleted.
/// </summary>
public record ProductAttributeDeletedEvent(
    Guid AttributeId) : DomainEvent;

/// <summary>
/// Raised when a value is added to an attribute.
/// </summary>
public record ProductAttributeValueAddedEvent(
    Guid AttributeId,
    Guid ValueId,
    string Value,
    string DisplayValue) : DomainEvent;

/// <summary>
/// Raised when an attribute value is updated.
/// </summary>
public record ProductAttributeValueUpdatedEvent(
    Guid AttributeId,
    Guid ValueId,
    string Value,
    string DisplayValue) : DomainEvent;

/// <summary>
/// Raised when an attribute value is removed.
/// </summary>
public record ProductAttributeValueRemovedEvent(
    Guid AttributeId,
    Guid ValueId) : DomainEvent;
