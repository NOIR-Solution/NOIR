namespace NOIR.Application.Features.Checkout.Commands.SelectShippingMethod;

/// <summary>
/// Command to select shipping method for a checkout session.
/// </summary>
public sealed record SelectShippingMethodCommand(
    Guid SessionId,
    string ShippingMethod,
    decimal ShippingCost,
    DateTimeOffset? EstimatedDeliveryAt = null) : IAuditableCommand<CheckoutSessionDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public object? GetTargetId() => SessionId;
    public AuditOperationType OperationType => AuditOperationType.Update;
    public string? GetTargetDisplayName() => ShippingMethod;
    public string? GetActionDescription() => $"Selected shipping method: {ShippingMethod}";
}
