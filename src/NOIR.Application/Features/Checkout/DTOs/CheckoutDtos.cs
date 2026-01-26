namespace NOIR.Application.Features.Checkout.DTOs;

/// <summary>
/// DTO for checkout session response.
/// </summary>
public record CheckoutSessionDto(
    Guid Id,
    Guid CartId,
    string? UserId,
    CheckoutSessionStatus Status,
    DateTimeOffset ExpiresAt,
    string CustomerEmail,
    string? CustomerName,
    string? CustomerPhone,
    AddressDto? ShippingAddress,
    AddressDto? BillingAddress,
    bool BillingSameAsShipping,
    string? ShippingMethod,
    decimal ShippingCost,
    DateTimeOffset? EstimatedDeliveryAt,
    PaymentMethod? PaymentMethod,
    Guid? PaymentGatewayId,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal GrandTotal,
    string Currency,
    string? CouponCode,
    string? CustomerNotes,
    Guid? OrderId,
    string? OrderNumber,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt);

/// <summary>
/// DTO for initiating checkout request.
/// </summary>
public record InitiateCheckoutRequest(
    Guid CartId,
    string CustomerEmail,
    string? CustomerName = null,
    string? CustomerPhone = null);

/// <summary>
/// DTO for setting checkout address (Vietnam format).
/// </summary>
public record SetCheckoutAddressRequest(
    string FullName,
    string Phone,
    string AddressLine1,
    string? AddressLine2,
    string? Ward,
    string? District,
    string? Province,
    string? PostalCode,
    string Country = "Vietnam");

/// <summary>
/// DTO for selecting shipping method.
/// </summary>
public record SelectShippingMethodRequest(
    string ShippingMethod,
    decimal ShippingCost,
    DateTimeOffset? EstimatedDeliveryAt = null);

/// <summary>
/// DTO for selecting payment method.
/// </summary>
public record SelectPaymentMethodRequest(
    PaymentMethod PaymentMethod,
    Guid? PaymentGatewayId = null);

/// <summary>
/// DTO for applying coupon.
/// </summary>
public record ApplyCouponRequest(
    string CouponCode);

/// <summary>
/// DTO for completing checkout.
/// </summary>
public record CompleteCheckoutRequest(
    string? CustomerNotes = null);
