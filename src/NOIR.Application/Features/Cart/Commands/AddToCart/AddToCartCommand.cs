namespace NOIR.Application.Features.Cart.Commands.AddToCart;

/// <summary>
/// Command to add an item to the shopping cart.
/// </summary>
public sealed record AddToCartCommand(
    Guid ProductId,
    Guid ProductVariantId,
    int Quantity = 1) : IAuditableCommand<CartDto>
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

    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => UserId;
    public string? GetTargetDisplayName() => null;
    public string? GetActionDescription() => $"Added {Quantity} item(s) to cart";
}
