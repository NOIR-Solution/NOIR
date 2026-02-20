namespace NOIR.Application.Features.Payments.Commands.RefreshPaymentStatus;

/// <summary>
/// Command to refresh payment status from the gateway.
/// </summary>
public sealed record RefreshPaymentStatusCommand(
    Guid PaymentTransactionId) : IAuditableCommand<PaymentTransactionDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => PaymentTransactionId;
    public string? GetTargetDisplayName() => "Payment";
    public string? GetActionDescription() => "Refreshed payment status from gateway";
}
