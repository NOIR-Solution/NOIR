namespace NOIR.Application.Features.Wishlists.Commands.DeleteWishlist;

/// <summary>
/// Command to soft delete a wishlist. Cannot delete the default wishlist.
/// </summary>
public sealed record DeleteWishlistCommand(Guid Id) : IAuditableCommand<WishlistDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Delete;
    public object? GetTargetId() => Id;
    public string? GetTargetDisplayName() => null;
    public string? GetActionDescription() => "Deleted wishlist";
}
