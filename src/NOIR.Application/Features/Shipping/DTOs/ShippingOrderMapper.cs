namespace NOIR.Application.Features.Shipping.DTOs;

/// <summary>
/// Extension methods for mapping ShippingOrder entity to DTOs.
/// </summary>
public static class ShippingOrderMapper
{
    public static ShippingOrderDto ToDto(this ShippingOrder order, string providerName)
    {
        return new ShippingOrderDto(
            order.Id,
            order.OrderId,
            order.ProviderCode,
            providerName,
            order.ProviderOrderId,
            order.TrackingNumber,
            order.ServiceTypeCode,
            order.ServiceTypeName,
            order.Status,
            order.BaseRate,
            order.CodFee,
            order.InsuranceFee,
            order.TotalShippingFee,
            order.CodAmount,
            order.PickupAddressJson,
            order.DeliveryAddressJson,
            order.LabelUrl,
            order.TrackingUrl,
            order.EstimatedDeliveryDate,
            order.ActualDeliveryDate,
            order.CreatedAt,
            order.ModifiedAt);
    }
}
