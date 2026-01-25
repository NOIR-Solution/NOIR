namespace NOIR.Application.Features.Payments.Commands.CancelPayment;

/// <summary>
/// Command to cancel a pending payment.
/// </summary>
public sealed record CancelPaymentCommand(
    Guid PaymentTransactionId,
    string? Reason) : IAuditableCommand<PaymentTransactionDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => UserId;
    public string? GetTargetDisplayName() => $"Payment {PaymentTransactionId}";
    public string? GetActionDescription() => $"Cancelled payment {PaymentTransactionId}";
}
