namespace NOIR.Application.Features.Shipping.Specifications;

/// <summary>
/// Get shipping provider by ID (read-only).
/// </summary>
public sealed class ShippingProviderByIdSpec : Specification<ShippingProvider>
{
    public ShippingProviderByIdSpec(Guid id)
    {
        Query.Where(p => p.Id == id)
             .TagWith("ShippingProviderById");
    }
}

/// <summary>
/// Get shipping provider by ID for update (with tracking).
/// </summary>
public sealed class ShippingProviderByIdForUpdateSpec : Specification<ShippingProvider>
{
    public ShippingProviderByIdForUpdateSpec(Guid id)
    {
        Query.Where(p => p.Id == id)
             .AsTracking()
             .TagWith("ShippingProviderByIdForUpdate");
    }
}

/// <summary>
/// Get all active shipping providers (for checkout display).
/// </summary>
public sealed class ActiveShippingProvidersSpec : Specification<ShippingProvider>
{
    public ActiveShippingProvidersSpec()
    {
        Query.Where(p => p.IsActive)
             .OrderBy(p => (object)p.SortOrder)
             .TagWith("ActiveShippingProviders");
    }
}

/// <summary>
/// Get shipping provider by provider code.
/// </summary>
public sealed class ShippingProviderByCodeSpec : Specification<ShippingProvider>
{
    public ShippingProviderByCodeSpec(ShippingProviderCode providerCode)
    {
        Query.Where(p => p.ProviderCode == providerCode)
             .TagWith("ShippingProviderByCode");
    }
}

/// <summary>
/// Get all shipping providers (admin view).
/// </summary>
public sealed class ShippingProvidersSpec : Specification<ShippingProvider>
{
    public ShippingProvidersSpec()
    {
        Query.OrderBy(p => (object)p.SortOrder)
             .TagWith("GetShippingProviders");
    }
}
