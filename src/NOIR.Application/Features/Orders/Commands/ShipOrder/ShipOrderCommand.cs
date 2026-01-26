namespace NOIR.Application.Features.Orders.Commands.ShipOrder;

/// <summary>
/// Command to ship an order.
/// </summary>
public sealed record ShipOrderCommand(
    Guid OrderId,
    string TrackingNumber,
    string ShippingCarrier) : IAuditableCommand<OrderDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => OrderId;
    public string? GetTargetDisplayName() => "Order";
    public string? GetActionDescription() => $"Shipped order via {ShippingCarrier}";
}
