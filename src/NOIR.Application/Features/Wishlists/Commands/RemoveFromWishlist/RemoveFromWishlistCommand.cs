namespace NOIR.Application.Features.Wishlists.Commands.RemoveFromWishlist;

/// <summary>
/// Command to remove an item from a wishlist.
/// </summary>
public sealed record RemoveFromWishlistCommand(Guid WishlistItemId) : IAuditableCommand<WishlistDetailDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Delete;
    public object? GetTargetId() => WishlistItemId;
    public string? GetTargetDisplayName() => null;
    public string? GetActionDescription() => "Removed item from wishlist";
}
