namespace NOIR.Application.Features.Checkout.Commands.InitiateCheckout;

/// <summary>
/// Command to initiate a checkout session from a cart.
/// </summary>
public sealed record InitiateCheckoutCommand(
    Guid CartId,
    string CustomerEmail,
    string? CustomerName = null,
    string? CustomerPhone = null) : IAuditableCommand<CheckoutSessionDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public object? GetTargetId() => CartId;
    public AuditOperationType OperationType => AuditOperationType.Create;
    public string? GetTargetDisplayName() => $"Checkout for cart {CartId}";
    public string? GetActionDescription() => $"Initiated checkout session for cart";
}
