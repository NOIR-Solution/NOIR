namespace NOIR.Infrastructure.Services.Payment;

/// <summary>
/// Factory for resolving payment gateway providers.
/// </summary>
public class PaymentGatewayFactory : IPaymentGatewayFactory, IScopedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IRepository<PaymentGateway, Guid> _gatewayRepository;
    private readonly ICredentialEncryptionService _encryptionService;

    public PaymentGatewayFactory(
        IServiceProvider serviceProvider,
        IRepository<PaymentGateway, Guid> gatewayRepository,
        ICredentialEncryptionService encryptionService)
    {
        _serviceProvider = serviceProvider;
        _gatewayRepository = gatewayRepository;
        _encryptionService = encryptionService;
    }

    public IPaymentGatewayProvider? GetProvider(string provider)
    {
        // Get all registered providers and find matching one
        var providers = _serviceProvider.GetServices<IPaymentGatewayProvider>();
        return providers.FirstOrDefault(p => p.ProviderName.Equals(provider, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IPaymentGatewayProvider?> GetProviderWithCredentialsAsync(
        string provider,
        CancellationToken cancellationToken = default)
    {
        var gatewayProvider = GetProvider(provider);
        if (gatewayProvider == null)
        {
            return null;
        }

        // Get gateway configuration
        var spec = new PaymentGatewayByProviderSpec(provider);
        var gateway = await _gatewayRepository.FirstOrDefaultAsync(spec, cancellationToken);
        if (gateway == null)
        {
            return null;
        }

        // Decrypt credentials and initialize provider
        if (!string.IsNullOrEmpty(gateway.EncryptedCredentials))
        {
            var credentialsJson = _encryptionService.Decrypt(gateway.EncryptedCredentials);
            var credentials = JsonSerializer.Deserialize<Dictionary<string, string>>(credentialsJson)
                ?? new Dictionary<string, string>();
            await gatewayProvider.InitializeAsync(credentials, gateway.Environment, cancellationToken);
        }

        return gatewayProvider;
    }

    public IEnumerable<string> GetAvailableProviders()
    {
        var providers = _serviceProvider.GetServices<IPaymentGatewayProvider>();
        return providers.Select(p => p.ProviderName);
    }
}
