namespace NOIR.Application.Features.Orders.Commands.CompleteOrder;

/// <summary>
/// Command to complete an order.
/// </summary>
public sealed record CompleteOrderCommand(Guid OrderId) : IAuditableCommand<OrderDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => OrderId;
    public string? GetTargetDisplayName() => "Order";
    public string? GetActionDescription() => "Completed order";
}
