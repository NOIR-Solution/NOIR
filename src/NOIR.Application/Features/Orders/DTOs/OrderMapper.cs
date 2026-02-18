namespace NOIR.Application.Features.Orders.DTOs;

/// <summary>
/// Mapper for Order-related entities to DTOs.
/// </summary>
public static class OrderMapper
{
    /// <summary>
    /// Maps an Order entity to OrderDto.
    /// </summary>
    public static OrderDto ToDto(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            CustomerId = order.CustomerId,
            Status = order.Status,
            SubTotal = order.SubTotal,
            DiscountAmount = order.DiscountAmount,
            ShippingAmount = order.ShippingAmount,
            TaxAmount = order.TaxAmount,
            GrandTotal = order.GrandTotal,
            Currency = order.Currency,
            CustomerEmail = order.CustomerEmail,
            CustomerPhone = order.CustomerPhone,
            CustomerName = order.CustomerName,
            ShippingAddress = order.ShippingAddress is not null ? ToDto(order.ShippingAddress) : null,
            BillingAddress = order.BillingAddress is not null ? ToDto(order.BillingAddress) : null,
            ShippingMethod = order.ShippingMethod,
            TrackingNumber = order.TrackingNumber,
            ShippingCarrier = order.ShippingCarrier,
            EstimatedDeliveryAt = order.EstimatedDeliveryAt,
            CouponCode = order.CouponCode,
            CustomerNotes = order.CustomerNotes,
            Items = order.Items.Select(ToDto).ToList(),
            CreatedAt = order.CreatedAt,
            ConfirmedAt = order.ConfirmedAt,
            ShippedAt = order.ShippedAt,
            DeliveredAt = order.DeliveredAt,
            CompletedAt = order.CompletedAt,
            CancelledAt = order.CancelledAt,
            CancellationReason = order.CancellationReason,
            ReturnedAt = order.ReturnedAt,
            ReturnReason = order.ReturnReason
        };
    }

    /// <summary>
    /// Maps an Order entity to OrderSummaryDto.
    /// </summary>
    public static OrderSummaryDto ToSummaryDto(Order order)
    {
        return new OrderSummaryDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            Status = order.Status,
            GrandTotal = order.GrandTotal,
            Currency = order.Currency,
            CustomerEmail = order.CustomerEmail,
            CustomerName = order.CustomerName,
            ItemCount = order.Items.Count,
            CreatedAt = order.CreatedAt
        };
    }

    /// <summary>
    /// Maps an OrderItem entity to OrderItemDto.
    /// </summary>
    public static OrderItemDto ToDto(OrderItem item)
    {
        return new OrderItemDto
        {
            Id = item.Id,
            ProductId = item.ProductId,
            ProductVariantId = item.ProductVariantId,
            ProductName = item.ProductName,
            VariantName = item.VariantName,
            Sku = item.Sku,
            ImageUrl = item.ImageUrl,
            OptionsSnapshot = item.OptionsSnapshot,
            UnitPrice = item.UnitPrice,
            Quantity = item.Quantity,
            DiscountAmount = item.DiscountAmount,
            TaxAmount = item.TaxAmount,
            LineTotal = item.LineTotal,
            Subtotal = item.Subtotal
        };
    }

    /// <summary>
    /// Maps an Address value object to AddressDto.
    /// </summary>
    public static AddressDto ToDto(Address address)
    {
        return new AddressDto
        {
            FullName = address.FullName,
            Phone = address.Phone,
            AddressLine1 = address.AddressLine1,
            AddressLine2 = address.AddressLine2,
            Ward = address.Ward,
            District = address.District,
            Province = address.Province,
            Country = address.Country,
            PostalCode = address.PostalCode,
            IsDefault = address.IsDefault
        };
    }

    /// <summary>
    /// Maps a CreateAddressDto to Address value object.
    /// </summary>
    public static Address ToAddress(CreateAddressDto dto)
    {
        return new Address
        {
            FullName = dto.FullName,
            Phone = dto.Phone,
            AddressLine1 = dto.AddressLine1,
            AddressLine2 = dto.AddressLine2,
            Ward = dto.Ward,
            District = dto.District,
            Province = dto.Province,
            Country = dto.Country,
            PostalCode = dto.PostalCode,
            IsDefault = false
        };
    }
}
