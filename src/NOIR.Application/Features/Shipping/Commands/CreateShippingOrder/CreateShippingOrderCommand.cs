namespace NOIR.Application.Features.Shipping.Commands.CreateShippingOrder;

/// <summary>
/// Command to create a shipping order with a provider.
/// </summary>
public sealed record CreateShippingOrderCommand(
    Guid OrderId,
    ShippingProviderCode ProviderCode,
    string ServiceTypeCode,
    ShippingAddressDto PickupAddress,
    ShippingAddressDto DeliveryAddress,
    ShippingContactDto Sender,
    ShippingContactDto Recipient,
    List<ShippingItemDto> Items,
    decimal TotalWeightGrams,
    decimal DeclaredValue,
    decimal? CodAmount = null,
    bool IsFreeship = false,
    bool RequireInsurance = false,
    string? Notes = null) : IAuditableCommand<ShippingOrderDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => OrderId;
    public string? GetTargetDisplayName() => $"Order {OrderId}";
    public string? GetActionDescription() => $"Created shipping order for {Recipient.FullName} via {ProviderCode}";
}
