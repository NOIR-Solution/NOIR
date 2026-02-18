namespace NOIR.Application.Features.Wishlists.Commands.MoveToCart;

/// <summary>
/// Command to move a wishlist item to the shopping cart.
/// </summary>
public sealed record MoveToCartCommand(Guid WishlistItemId) : IAuditableCommand<WishlistDetailDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => WishlistItemId;
    public string? GetTargetDisplayName() => null;
    public string? GetActionDescription() => "Moved wishlist item to cart";
}
