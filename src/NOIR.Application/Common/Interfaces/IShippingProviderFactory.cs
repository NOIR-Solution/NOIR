namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Factory for creating shipping provider instances.
/// </summary>
public interface IShippingProviderFactory
{
    /// <summary>
    /// Get a shipping provider implementation by code.
    /// </summary>
    IShippingProvider? GetProvider(ShippingProviderCode code);

    /// <summary>
    /// Get all registered shipping providers.
    /// </summary>
    IEnumerable<IShippingProvider> GetAllProviders();

    /// <summary>
    /// Check if a provider is supported.
    /// </summary>
    bool IsProviderSupported(ShippingProviderCode code);
}
