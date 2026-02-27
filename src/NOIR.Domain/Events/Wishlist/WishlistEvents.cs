namespace NOIR.Domain.Events.Wishlist;

/// <summary>
/// Raised when a new wishlist is created.
/// </summary>
public record WishlistCreatedEvent(
    Guid WishlistId,
    string UserId,
    string Name) : DomainEvent;

/// <summary>
/// Raised when an item is added to a wishlist.
/// </summary>
public record WishlistItemAddedEvent(
    Guid WishlistId,
    Guid ProductId,
    Guid? ProductVariantId) : DomainEvent;

/// <summary>
/// Raised when an item is removed from a wishlist.
/// </summary>
public record WishlistItemRemovedEvent(
    Guid WishlistId,
    Guid WishlistItemId) : DomainEvent;

/// <summary>
/// Raised when a wishlist is shared (share token generated).
/// </summary>
public record WishlistSharedEvent(
    Guid WishlistId,
    string UserId) : DomainEvent;
