namespace NOIR.Application.Features.Wishlists.Commands.ShareWishlist;

/// <summary>
/// Command to generate a share token for a wishlist.
/// </summary>
public sealed record ShareWishlistCommand(Guid WishlistId) : IAuditableCommand<WishlistDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => WishlistId;
    public string? GetTargetDisplayName() => null;
    public string? GetActionDescription() => "Generated share link for wishlist";
}
