namespace NOIR.Application.Features.Orders.Commands.ManualCreateOrder;

/// <summary>
/// Command to manually create a new order (admin use, bypasses checkout flow).
/// </summary>
public sealed record ManualCreateOrderCommand(
    string CustomerEmail,
    string? CustomerName,
    string? CustomerPhone,
    Guid? CustomerId,
    List<ManualOrderItemDto> Items,
    CreateAddressDto? ShippingAddress,
    CreateAddressDto? BillingAddress,
    string? ShippingMethod,
    string? CouponCode,
    string? CustomerNotes,
    string? InternalNotes,
    PaymentMethod? PaymentMethod,
    PaymentStatus? InitialPaymentStatus,
    decimal ShippingAmount = 0,
    decimal DiscountAmount = 0,
    decimal TaxAmount = 0,
    string Currency = "VND") : IAuditableCommand<OrderDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => UserId;
    public string? GetTargetDisplayName() => CustomerEmail;
    public string? GetActionDescription() => $"Manually created order for '{CustomerEmail}'";
}
