namespace NOIR.Application.Features.Payments.Commands.RecordManualPayment;

/// <summary>
/// Command to record a manual/offline payment for an order.
/// </summary>
public sealed record RecordManualPaymentCommand(
    Guid OrderId,
    decimal Amount,
    string Currency,
    PaymentMethod PaymentMethod,
    string? ReferenceNumber,
    string? Notes,
    DateTimeOffset? PaidAt) : IAuditableCommand<PaymentTransactionDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => OrderId;
    public string? GetTargetDisplayName() => $"Manual payment for order";
    public string? GetActionDescription() => $"Recorded manual {PaymentMethod} payment of {Amount} {Currency}";
}
