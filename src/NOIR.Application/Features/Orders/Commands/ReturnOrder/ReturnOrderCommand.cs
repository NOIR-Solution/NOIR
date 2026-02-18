namespace NOIR.Application.Features.Orders.Commands.ReturnOrder;

/// <summary>
/// Command to return an order.
/// </summary>
public sealed record ReturnOrderCommand(
    Guid OrderId,
    string? Reason) : IAuditableCommand<OrderDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => OrderId;
    public string? GetTargetDisplayName() => "Order";
    public string? GetActionDescription() => "Returned order";
}
