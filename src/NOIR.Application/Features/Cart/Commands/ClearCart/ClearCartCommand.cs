namespace NOIR.Application.Features.Cart.Commands.ClearCart;

/// <summary>
/// Command to clear all items from a cart.
/// </summary>
public sealed record ClearCartCommand(Guid CartId) : IAuditableCommand<CartDto>
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
    public string? GetActionDescription() => "Cleared all items from cart";
}
