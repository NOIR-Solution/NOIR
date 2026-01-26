namespace NOIR.Application.Features.Orders.Commands.ConfirmOrder;

/// <summary>
/// Command to confirm an order (payment received).
/// </summary>
public sealed record ConfirmOrderCommand(Guid OrderId) : IAuditableCommand<OrderDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => OrderId;
    public string? GetTargetDisplayName() => "Order";
    public string? GetActionDescription() => "Confirmed order";
}
