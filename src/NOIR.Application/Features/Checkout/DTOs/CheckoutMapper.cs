namespace NOIR.Application.Features.Checkout.DTOs;

/// <summary>
/// Maps CheckoutSession entities to DTOs.
/// </summary>
public static class CheckoutMapper
{
    public static CheckoutSessionDto ToDto(CheckoutSession session)
    {
        return new CheckoutSessionDto(
            Id: session.Id,
            CartId: session.CartId,
            UserId: session.UserId,
            Status: session.Status,
            ExpiresAt: session.ExpiresAt,
            CustomerEmail: session.CustomerEmail,
            CustomerName: session.CustomerName,
            CustomerPhone: session.CustomerPhone,
            ShippingAddress: session.ShippingAddress is not null
                ? new AddressDto
                {
                    FullName = session.ShippingAddress.FullName,
                    Phone = session.ShippingAddress.Phone,
                    AddressLine1 = session.ShippingAddress.AddressLine1,
                    AddressLine2 = session.ShippingAddress.AddressLine2,
                    Ward = session.ShippingAddress.Ward,
                    District = session.ShippingAddress.District,
                    Province = session.ShippingAddress.Province,
                    Country = session.ShippingAddress.Country,
                    PostalCode = session.ShippingAddress.PostalCode,
                    IsDefault = session.ShippingAddress.IsDefault
                }
                : null,
            BillingAddress: session.BillingAddress is not null
                ? new AddressDto
                {
                    FullName = session.BillingAddress.FullName,
                    Phone = session.BillingAddress.Phone,
                    AddressLine1 = session.BillingAddress.AddressLine1,
                    AddressLine2 = session.BillingAddress.AddressLine2,
                    Ward = session.BillingAddress.Ward,
                    District = session.BillingAddress.District,
                    Province = session.BillingAddress.Province,
                    Country = session.BillingAddress.Country,
                    PostalCode = session.BillingAddress.PostalCode,
                    IsDefault = session.BillingAddress.IsDefault
                }
                : null,
            BillingSameAsShipping: session.BillingSameAsShipping,
            ShippingMethod: session.ShippingMethod,
            ShippingCost: session.ShippingCost,
            EstimatedDeliveryAt: session.EstimatedDeliveryAt,
            PaymentMethod: session.PaymentMethod,
            PaymentGatewayId: session.PaymentGatewayId,
            SubTotal: session.SubTotal,
            DiscountAmount: session.DiscountAmount,
            TaxAmount: session.TaxAmount,
            GrandTotal: session.GrandTotal,
            Currency: session.Currency,
            CouponCode: session.CouponCode,
            CustomerNotes: session.CustomerNotes,
            OrderId: session.OrderId,
            OrderNumber: session.OrderNumber,
            CreatedAt: session.CreatedAt,
            ModifiedAt: session.ModifiedAt);
    }
}
