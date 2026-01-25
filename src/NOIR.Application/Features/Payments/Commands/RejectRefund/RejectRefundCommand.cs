namespace NOIR.Application.Features.Payments.Commands.RejectRefund;

/// <summary>
/// Command to reject a pending refund request.
/// </summary>
public sealed record RejectRefundCommand(
    Guid RefundId,
    string RejectionReason) : IAuditableCommand<RefundDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => UserId;
    public string? GetTargetDisplayName() => $"Refund {RefundId}";
    public string? GetActionDescription() => $"Rejected refund request {RefundId}";
}
