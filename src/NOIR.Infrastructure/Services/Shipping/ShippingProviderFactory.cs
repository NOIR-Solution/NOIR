namespace NOIR.Infrastructure.Services.Shipping;

/// <summary>
/// Factory for creating and managing shipping provider instances.
/// </summary>
public class ShippingProviderFactory : IShippingProviderFactory, IScopedService
{
    private readonly IEnumerable<IShippingProvider> _providers;
    private readonly Dictionary<ShippingProviderCode, IShippingProvider> _providerMap;

    public ShippingProviderFactory(IEnumerable<IShippingProvider> providers)
    {
        _providers = providers;
        _providerMap = providers.ToDictionary(p => p.ProviderCode);
    }

    public IShippingProvider? GetProvider(ShippingProviderCode code)
    {
        return _providerMap.GetValueOrDefault(code);
    }

    public IEnumerable<IShippingProvider> GetAllProviders()
    {
        return _providers;
    }

    public bool IsProviderSupported(ShippingProviderCode code)
    {
        return _providerMap.ContainsKey(code);
    }
}
