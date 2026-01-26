namespace NOIR.Application.Features.Cart.Commands.RemoveCartItem;

/// <summary>
/// Command to remove an item from the cart.
/// </summary>
public sealed record RemoveCartItemCommand(
    Guid CartId,
    Guid ItemId) : IAuditableCommand<CartDto>
{
    /// <summary>
    /// User ID if authenticated.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    /// <summary>
    /// Session ID for guest users.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string? SessionId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Delete;
    public object? GetTargetId() => CartId;
    public string? GetTargetDisplayName() => null;
    public string? GetActionDescription() => "Removed item from cart";
}
