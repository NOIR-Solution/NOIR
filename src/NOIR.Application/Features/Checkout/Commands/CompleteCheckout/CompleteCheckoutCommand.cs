namespace NOIR.Application.Features.Checkout.Commands.CompleteCheckout;

/// <summary>
/// Command to complete a checkout session and create an order.
/// </summary>
public sealed record CompleteCheckoutCommand(
    Guid SessionId,
    string? CustomerNotes = null) : IAuditableCommand<CheckoutSessionDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public object? GetTargetId() => SessionId;
    public AuditOperationType OperationType => AuditOperationType.Create;
    public string? GetTargetDisplayName() => $"Checkout {SessionId}";
    public string? GetActionDescription() => "Completed checkout and created order";
}
