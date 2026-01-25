namespace NOIR.Application.Features.Payments.Commands.ConfirmCodCollection;

/// <summary>
/// Command to confirm COD (Cash on Delivery) payment collection.
/// </summary>
public sealed record ConfirmCodCollectionCommand(
    Guid PaymentTransactionId,
    string? Notes) : IAuditableCommand<PaymentTransactionDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => UserId;
    public string? GetTargetDisplayName() => $"COD Payment {PaymentTransactionId}";
    public string? GetActionDescription() => $"Confirmed COD collection for payment {PaymentTransactionId}";
}
