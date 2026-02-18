namespace NOIR.Application.Features.Wishlists.Commands.UpdateWishlistItemPriority;

/// <summary>
/// Command to update the priority of a wishlist item.
/// </summary>
public sealed record UpdateWishlistItemPriorityCommand(
    Guid WishlistItemId,
    WishlistItemPriority Priority) : IAuditableCommand<WishlistDetailDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => WishlistItemId;
    public string? GetTargetDisplayName() => null;
    public string? GetActionDescription() => $"Updated wishlist item priority to {Priority}";
}
