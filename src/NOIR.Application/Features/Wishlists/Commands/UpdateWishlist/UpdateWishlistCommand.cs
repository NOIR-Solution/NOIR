namespace NOIR.Application.Features.Wishlists.Commands.UpdateWishlist;

/// <summary>
/// Command to update a wishlist.
/// </summary>
public sealed record UpdateWishlistCommand(
    Guid Id,
    string Name,
    bool IsPublic) : IAuditableCommand<WishlistDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => Id;
    public string? GetTargetDisplayName() => Name;
    public string? GetActionDescription() => $"Updated wishlist '{Name}'";
}
