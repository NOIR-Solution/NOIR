namespace NOIR.Application.Features.Checkout.Commands.SetCheckoutAddress;

/// <summary>
/// Command to set shipping/billing address on a checkout session.
/// </summary>
public sealed record SetCheckoutAddressCommand(
    Guid SessionId,
    string AddressType, // "Shipping" or "Billing"
    string FullName,
    string Phone,
    string AddressLine1,
    string? AddressLine2,
    string? Ward,
    string? District,
    string? Province,
    string? PostalCode,
    string Country = "Vietnam",
    bool BillingSameAsShipping = true) : IAuditableCommand<CheckoutSessionDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public object? GetTargetId() => SessionId;
    public AuditOperationType OperationType => AuditOperationType.Update;
    public string? GetTargetDisplayName() => $"{AddressType} address";
    public string? GetActionDescription() => $"Set {AddressType.ToLower()} address for checkout";
}
