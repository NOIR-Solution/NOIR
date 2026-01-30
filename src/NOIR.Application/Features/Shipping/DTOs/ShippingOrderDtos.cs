namespace NOIR.Application.Features.Shipping.DTOs;

/// <summary>
/// Item being shipped.
/// </summary>
public record ShippingItemDto(
    string Name,
    int Quantity,
    decimal WeightGrams,
    decimal Value,
    string? Sku = null);

/// <summary>
/// Request to create a shipping order.
/// </summary>
public record CreateShippingOrderRequest(
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
    string? Notes = null,
    DateTime? RequestedPickupDate = null);

/// <summary>
/// Response after creating a shipping order.
/// </summary>
public record ShippingOrderDto(
    Guid Id,
    Guid OrderId,
    ShippingProviderCode ProviderCode,
    string ProviderName,
    string? ProviderOrderId,
    string TrackingNumber,
    string ServiceTypeCode,
    string ServiceTypeName,
    ShippingStatus Status,
    decimal BaseRate,
    decimal CodFee,
    decimal InsuranceFee,
    decimal TotalShippingFee,
    decimal? CodAmount,
    string PickupAddressJson,
    string DeliveryAddressJson,
    string? LabelUrl,
    string? TrackingUrl,
    DateTimeOffset? EstimatedDeliveryDate,
    DateTimeOffset? ActualDeliveryDate,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt);

/// <summary>
/// Summary DTO for shipping order lists.
/// </summary>
public record ShippingOrderSummaryDto(
    Guid Id,
    Guid OrderId,
    string TrackingNumber,
    ShippingProviderCode ProviderCode,
    string ProviderName,
    ShippingStatus Status,
    decimal TotalShippingFee,
    DateTimeOffset? EstimatedDeliveryDate,
    DateTimeOffset CreatedAt);
