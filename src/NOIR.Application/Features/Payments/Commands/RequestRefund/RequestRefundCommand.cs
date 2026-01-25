namespace NOIR.Application.Features.Payments.Commands.RequestRefund;

/// <summary>
/// Command to request a refund for a payment.
/// </summary>
public sealed record RequestRefundCommand(
    Guid PaymentTransactionId,
    decimal Amount,
    RefundReason Reason,
    string? Notes) : IAuditableCommand<RefundDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => UserId;
    public string? GetTargetDisplayName() => $"Refund for Payment {PaymentTransactionId}";
    public string? GetActionDescription() => $"Requested refund of {Amount} for payment {PaymentTransactionId}";
}
