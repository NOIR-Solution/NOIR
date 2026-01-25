namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Factory for resolving payment gateway providers by name.
/// </summary>
public interface IPaymentGatewayFactory
{
    /// <summary>
    /// Gets a payment provider by name.
    /// </summary>
    IPaymentGatewayProvider? GetProvider(string providerName);

    /// <summary>
    /// Gets a payment provider with credentials initialized from the database.
    /// </summary>
    Task<IPaymentGatewayProvider?> GetProviderWithCredentialsAsync(
        string providerName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available provider names.
    /// </summary>
    IEnumerable<string> GetAvailableProviders();
}
