namespace NOIR.Application.Features.Orders.Commands.CreateOrder;

/// <summary>
/// Command to create a new order.
/// </summary>
public sealed record CreateOrderCommand(
    string CustomerEmail,
    string? CustomerName,
    string? CustomerPhone,
    CreateAddressDto ShippingAddress,
    CreateAddressDto? BillingAddress,
    string? ShippingMethod,
    decimal ShippingAmount,
    string? CouponCode,
    decimal DiscountAmount,
    string? CustomerNotes,
    List<CreateOrderItemDto> Items,
    string Currency = "VND",
    Guid? CheckoutSessionId = null) : IAuditableCommand<OrderDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => UserId;
    public string? GetTargetDisplayName() => CustomerEmail;
    public string? GetActionDescription() => $"Created order for '{CustomerEmail}'";
}
