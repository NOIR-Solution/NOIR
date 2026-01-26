namespace NOIR.Application.Features.Orders.DTOs;

/// <summary>
/// DTO for Order entity.
/// </summary>
public sealed record OrderDto
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public Guid? CustomerId { get; init; }
    public OrderStatus Status { get; init; }

    // Financial
    public decimal SubTotal { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal ShippingAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal GrandTotal { get; init; }
    public string Currency { get; init; } = "VND";

    // Customer
    public string CustomerEmail { get; init; } = string.Empty;
    public string? CustomerPhone { get; init; }
    public string? CustomerName { get; init; }

    // Addresses
    public AddressDto? ShippingAddress { get; init; }
    public AddressDto? BillingAddress { get; init; }

    // Shipping
    public string? ShippingMethod { get; init; }
    public string? TrackingNumber { get; init; }
    public string? ShippingCarrier { get; init; }
    public DateTimeOffset? EstimatedDeliveryAt { get; init; }

    // Coupon
    public string? CouponCode { get; init; }

    // Notes
    public string? CustomerNotes { get; init; }

    // Items
    public IReadOnlyList<OrderItemDto> Items { get; init; } = [];

    // Timestamps
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ConfirmedAt { get; init; }
    public DateTimeOffset? ShippedAt { get; init; }
    public DateTimeOffset? DeliveredAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public DateTimeOffset? CancelledAt { get; init; }
    public string? CancellationReason { get; init; }
}

/// <summary>
/// DTO for OrderItem entity.
/// </summary>
public sealed record OrderItemDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public Guid ProductVariantId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string VariantName { get; init; } = string.Empty;
    public string? Sku { get; init; }
    public string? ImageUrl { get; init; }
    public string? OptionsSnapshot { get; init; }
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal LineTotal { get; init; }
    public decimal Subtotal { get; init; }
}

/// <summary>
/// DTO for Address value object.
/// </summary>
public sealed record AddressDto
{
    public string FullName { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string AddressLine1 { get; init; } = string.Empty;
    public string? AddressLine2 { get; init; }
    public string Ward { get; init; } = string.Empty;
    public string District { get; init; } = string.Empty;
    public string Province { get; init; } = string.Empty;
    public string Country { get; init; } = "Vietnam";
    public string? PostalCode { get; init; }
    public bool IsDefault { get; init; }
}

/// <summary>
/// Summary DTO for order lists.
/// </summary>
public sealed record OrderSummaryDto
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public OrderStatus Status { get; init; }
    public decimal GrandTotal { get; init; }
    public string Currency { get; init; } = "VND";
    public string CustomerEmail { get; init; } = string.Empty;
    public string? CustomerName { get; init; }
    public int ItemCount { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// Request DTO for creating an order item.
/// </summary>
public sealed record CreateOrderItemDto(
    Guid ProductId,
    Guid ProductVariantId,
    string ProductName,
    string VariantName,
    decimal UnitPrice,
    int Quantity,
    string? Sku = null,
    string? ImageUrl = null,
    string? OptionsSnapshot = null);

/// <summary>
/// Request DTO for creating an address.
/// </summary>
public sealed record CreateAddressDto(
    string FullName,
    string Phone,
    string AddressLine1,
    string? AddressLine2,
    string Ward,
    string District,
    string Province,
    string Country = "Vietnam",
    string? PostalCode = null);
