namespace NOIR.Application.Features.Checkout.Commands.SelectPaymentMethod;

/// <summary>
/// Command to select payment method for a checkout session.
/// </summary>
public sealed record SelectPaymentMethodCommand(
    Guid SessionId,
    PaymentMethod PaymentMethod,
    Guid? PaymentGatewayId = null) : IAuditableCommand<CheckoutSessionDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public object? GetTargetId() => SessionId;
    public AuditOperationType OperationType => AuditOperationType.Update;
    public string? GetTargetDisplayName() => PaymentMethod.ToString();
    public string? GetActionDescription() => $"Selected payment method: {PaymentMethod}";
}
