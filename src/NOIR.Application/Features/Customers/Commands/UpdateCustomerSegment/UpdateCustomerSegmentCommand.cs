namespace NOIR.Application.Features.Customers.Commands.UpdateCustomerSegment;

/// <summary>
/// Command to manually override a customer's segment.
/// </summary>
public sealed record UpdateCustomerSegmentCommand(
    Guid Id,
    CustomerSegment Segment) : IAuditableCommand<CustomerDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => Id;
    public string? GetTargetDisplayName() => "Customer";
    public string? GetActionDescription() => $"Updated customer segment to '{Segment}'";
}
