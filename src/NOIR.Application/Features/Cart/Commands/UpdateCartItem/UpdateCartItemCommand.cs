namespace NOIR.Application.Features.Cart.Commands.UpdateCartItem;

/// <summary>
/// Command to update quantity of a cart item.
/// </summary>
public sealed record UpdateCartItemCommand(
    Guid CartId,
    Guid ItemId,
    int Quantity) : IAuditableCommand<CartDto>
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

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => CartId;
    public string? GetTargetDisplayName() => null;
    public string? GetActionDescription() => Quantity == 0
        ? "Removed item from cart"
        : $"Updated cart item quantity to {Quantity}";
}
