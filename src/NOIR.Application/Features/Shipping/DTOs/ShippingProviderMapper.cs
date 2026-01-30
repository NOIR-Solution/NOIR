namespace NOIR.Application.Features.Shipping.DTOs;

/// <summary>
/// Mapper for ShippingProvider entity to DTOs.
/// </summary>
public static class ShippingProviderMapper
{
    public static ShippingProviderDto ToDto(this ShippingProvider provider)
    {
        return new ShippingProviderDto(
            provider.Id,
            provider.ProviderCode,
            provider.ProviderName,
            provider.DisplayName,
            provider.IsActive,
            provider.SortOrder,
            provider.Environment,
            !string.IsNullOrEmpty(provider.EncryptedCredentials),
            provider.WebhookUrl,
            provider.ApiBaseUrl,
            provider.TrackingUrlTemplate,
            provider.SupportedServices,
            provider.MinWeightGrams,
            provider.MaxWeightGrams,
            provider.MinCodAmount,
            provider.MaxCodAmount,
            provider.SupportsCod,
            provider.SupportsInsurance,
            provider.LastHealthCheck,
            provider.HealthStatus,
            provider.CreatedAt,
            provider.ModifiedAt);
    }

    public static CheckoutShippingProviderDto ToCheckoutDto(this ShippingProvider provider)
    {
        return new CheckoutShippingProviderDto(
            provider.Id,
            provider.ProviderCode,
            provider.ProviderName,
            provider.DisplayName,
            provider.SortOrder,
            provider.SupportedServices,
            provider.SupportsCod,
            provider.SupportsInsurance);
    }

    public static ShippingProviderListDto ToListDto(this ShippingProvider provider)
    {
        return new ShippingProviderListDto(
            provider.Id,
            provider.ProviderCode,
            provider.ProviderName,
            provider.DisplayName,
            provider.IsActive,
            provider.SortOrder,
            provider.Environment,
            provider.HealthStatus);
    }
}
