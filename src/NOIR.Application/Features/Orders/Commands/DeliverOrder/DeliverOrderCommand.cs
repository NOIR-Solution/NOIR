namespace NOIR.Application.Features.Orders.Commands.DeliverOrder;

/// <summary>
/// Command to mark an order as delivered.
/// </summary>
public sealed record DeliverOrderCommand(Guid OrderId) : IAuditableCommand<OrderDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => OrderId;
    public string? GetTargetDisplayName() => "Order";
    public string? GetActionDescription() => "Delivered order";
}
