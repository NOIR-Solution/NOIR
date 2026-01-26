namespace NOIR.Application.Features.Orders.Commands.CancelOrder;

/// <summary>
/// Command to cancel an order.
/// </summary>
public sealed record CancelOrderCommand(
    Guid OrderId,
    string? Reason) : IAuditableCommand<OrderDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => OrderId;
    public string? GetTargetDisplayName() => "Order";
    public string? GetActionDescription() => "Cancelled order";
}
