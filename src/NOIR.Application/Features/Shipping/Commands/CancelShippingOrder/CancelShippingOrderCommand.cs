namespace NOIR.Application.Features.Shipping.Commands.CancelShippingOrder;

/// <summary>
/// Command to cancel a shipping order.
/// </summary>
public sealed record CancelShippingOrderCommand(
    string TrackingNumber,
    string? Reason = null) : IAuditableCommand
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Delete;
    public object? GetTargetId() => TrackingNumber;
    public string? GetTargetDisplayName() => TrackingNumber;
    public string? GetActionDescription() => $"Cancelled shipping order {TrackingNumber}: {Reason ?? "No reason provided"}";
}
