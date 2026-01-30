namespace NOIR.Application.Features.Shipping.Commands.ConfigureShippingProvider;

/// <summary>
/// Handler for configuring a shipping provider.
/// </summary>
public class ConfigureShippingProviderCommandHandler
{
    private readonly IRepository<ShippingProvider, Guid> _providerRepository;
    private readonly ICredentialEncryptionService _encryptionService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public ConfigureShippingProviderCommandHandler(
        IRepository<ShippingProvider, Guid> providerRepository,
        ICredentialEncryptionService encryptionService,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _providerRepository = providerRepository;
        _encryptionService = encryptionService;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<ShippingProviderDto>> Handle(
        ConfigureShippingProviderCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        // Check if provider with this code already exists for this tenant
        var providerSpec = new ShippingProviderByCodeSpec(command.ProviderCode);
        var existing = await _providerRepository.FirstOrDefaultAsync(providerSpec, cancellationToken);
        if (existing != null)
        {
            return Result.Failure<ShippingProviderDto>(
                Error.Conflict($"Shipping provider '{command.ProviderCode}' already exists.", ErrorCodes.Shipping.ProviderAlreadyExists));
        }

        // Get provider name from code
        var providerName = ShippingProviderMetadata.GetProviderName(command.ProviderCode);

        // Encrypt credentials
        var credentialsJson = JsonSerializer.Serialize(command.Credentials);
        var encryptedCredentials = _encryptionService.Encrypt(credentialsJson);

        // Create provider
        var provider = ShippingProvider.Create(
            command.ProviderCode,
            command.DisplayName,
            providerName,
            command.Environment,
            tenantId);

        // Configure credentials
        provider.Configure(encryptedCredentials, null);
        provider.SetSortOrder(command.SortOrder);
        provider.SetCodSupport(command.SupportsCod);
        provider.SetInsuranceSupport(command.SupportsInsurance);

        // Set supported services as JSON
        var servicesJson = JsonSerializer.Serialize(command.SupportedServices);
        provider.SetSupportedServices(servicesJson);

        // Set optional configuration
        if (!string.IsNullOrEmpty(command.ApiBaseUrl))
        {
            provider.SetApiBaseUrl(command.ApiBaseUrl);
        }

        if (!string.IsNullOrEmpty(command.TrackingUrlTemplate))
        {
            provider.SetTrackingUrlTemplate(command.TrackingUrlTemplate);
        }

        // Set default tracking URL templates if not provided
        if (string.IsNullOrEmpty(command.TrackingUrlTemplate))
        {
            var defaultTrackingUrl = ShippingProviderMetadata.GetDefaultTrackingUrlTemplate(command.ProviderCode);
            if (!string.IsNullOrEmpty(defaultTrackingUrl))
            {
                provider.SetTrackingUrlTemplate(defaultTrackingUrl);
            }
        }

        if (command.IsActive)
        {
            provider.Activate();
        }

        await _providerRepository.AddAsync(provider, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(provider.ToDto());
    }
}
