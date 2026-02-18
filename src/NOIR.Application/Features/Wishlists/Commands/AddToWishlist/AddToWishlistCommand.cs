namespace NOIR.Application.Features.Wishlists.Commands.AddToWishlist;

/// <summary>
/// Command to add a product to a wishlist.
/// If no WishlistId is provided, the default wishlist is used (created if needed).
/// </summary>
public sealed record AddToWishlistCommand(
    Guid? WishlistId,
    Guid ProductId,
    Guid? ProductVariantId = null,
    string? Note = null) : IAuditableCommand<WishlistDetailDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => UserId;
    public string? GetTargetDisplayName() => null;
    public string? GetActionDescription() => "Added product to wishlist";
}
