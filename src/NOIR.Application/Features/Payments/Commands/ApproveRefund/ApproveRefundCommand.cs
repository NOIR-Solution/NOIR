namespace NOIR.Application.Features.Payments.Commands.ApproveRefund;

/// <summary>
/// Command to approve a pending refund request.
/// </summary>
public sealed record ApproveRefundCommand(
    Guid RefundId,
    string? Notes) : IAuditableCommand<RefundDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => UserId;
    public string? GetTargetDisplayName() => $"Refund {RefundId}";
    public string? GetActionDescription() => $"Approved refund request {RefundId}";
}
