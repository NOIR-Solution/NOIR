namespace NOIR.Application.Features.Payments.Commands.CreatePayment;

/// <summary>
/// Command to initiate a payment for an order.
/// </summary>
public sealed record CreatePaymentCommand(
    Guid OrderId,
    decimal Amount,
    string Currency,
    PaymentMethod PaymentMethod,
    string Provider,
    string? ReturnUrl,
    string? IdempotencyKey,
    Dictionary<string, string>? Metadata) : IAuditableCommand<PaymentTransactionDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => UserId;
    public string? GetTargetDisplayName() => $"Payment for Order {OrderId}";
    public string? GetActionDescription() => $"Initiated {PaymentMethod} payment of {Amount} {Currency}";
}
