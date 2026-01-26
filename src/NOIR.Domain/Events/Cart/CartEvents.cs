namespace NOIR.Domain.Events.Cart;

/// <summary>
/// Raised when a new cart is created.
/// </summary>
public record CartCreatedEvent(
    Guid CartId,
    string? UserId,
    string? SessionId) : DomainEvent;

/// <summary>
/// Raised when an item is added to a cart.
/// </summary>
public record CartItemAddedEvent(
    Guid CartId,
    Guid CartItemId,
    Guid ProductId,
    Guid ProductVariantId,
    int Quantity) : DomainEvent;

/// <summary>
/// Raised when an item is removed from a cart.
/// </summary>
public record CartItemRemovedEvent(
    Guid CartId,
    Guid CartItemId,
    Guid ProductId,
    Guid ProductVariantId) : DomainEvent;

/// <summary>
/// Raised when an item quantity is updated in a cart.
/// </summary>
public record CartItemQuantityUpdatedEvent(
    Guid CartId,
    Guid CartItemId,
    int OldQuantity,
    int NewQuantity) : DomainEvent;

/// <summary>
/// Raised when a cart is abandoned (no activity for 30+ minutes).
/// </summary>
public record CartAbandonedEvent(
    Guid CartId,
    string? UserId,
    string? SessionId,
    int ItemCount,
    decimal Subtotal) : DomainEvent;

/// <summary>
/// Raised when a cart is converted to an order.
/// </summary>
public record CartConvertedEvent(
    Guid CartId,
    Guid OrderId,
    string? UserId,
    int ItemCount,
    decimal Subtotal) : DomainEvent;

/// <summary>
/// Raised when a guest cart is merged into a user cart.
/// </summary>
public record CartMergedEvent(
    Guid SourceCartId,
    Guid TargetCartId,
    string UserId,
    int MergedItemCount) : DomainEvent;
