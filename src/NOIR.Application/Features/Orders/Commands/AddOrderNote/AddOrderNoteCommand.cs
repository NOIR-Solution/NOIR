namespace NOIR.Application.Features.Orders.Commands.AddOrderNote;

/// <summary>
/// Command to add an internal note to an order.
/// </summary>
public sealed record AddOrderNoteCommand(
    Guid OrderId,
    string Content) : IAuditableCommand<OrderNoteDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => OrderId;
    public string? GetTargetDisplayName() => "Order Note";
    public string? GetActionDescription() => "Added note to order";
}
