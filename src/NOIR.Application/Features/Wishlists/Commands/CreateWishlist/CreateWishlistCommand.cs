namespace NOIR.Application.Features.Wishlists.Commands.CreateWishlist;

/// <summary>
/// Command to create a new wishlist.
/// </summary>
public sealed record CreateWishlistCommand(
    string Name,
    bool IsPublic = false) : IAuditableCommand<WishlistDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => UserId;
    public string? GetTargetDisplayName() => Name;
    public string? GetActionDescription() => $"Created wishlist '{Name}'";
}
