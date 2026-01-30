namespace NOIR.Application.Features.Shipping.Commands.UpdateShippingProvider;

/// <summary>
/// Handler for updating a shipping provider.
/// </summary>
public class UpdateShippingProviderCommandHandler
{
    private readonly IRepository<ShippingProvider, Guid> _providerRepository;
    private readonly ICredentialEncryptionService _encryptionService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateShippingProviderCommandHandler(
        IRepository<ShippingProvider, Guid> providerRepository,
        ICredentialEncryptionService encryptionService,
        IUnitOfWork unitOfWork)
    {
        _providerRepository = providerRepository;
        _encryptionService = encryptionService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ShippingProviderDto>> Handle(
        UpdateShippingProviderCommand command,
        CancellationToken cancellationToken)
    {
        // Get provider with tracking
        var spec = new ShippingProviderByIdForUpdateSpec(command.ProviderId);
        var provider = await _providerRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (provider == null)
        {
            return Result.Failure<ShippingProviderDto>(
                Error.NotFound("Shipping provider not found.", ErrorCodes.Shipping.ProviderNotFound));
        }

        // Update display name
        if (!string.IsNullOrEmpty(command.DisplayName))
        {
            provider.UpdateDisplayName(command.DisplayName);
        }

        // Update environment
        if (command.Environment.HasValue)
        {
            provider.UpdateEnvironment(command.Environment.Value);
        }

        // Update credentials
        if (command.Credentials != null && command.Credentials.Count > 0)
        {
            var credentialsJson = JsonSerializer.Serialize(command.Credentials);
            var encryptedCredentials = _encryptionService.Encrypt(credentialsJson);
            provider.UpdateCredentials(encryptedCredentials);
        }

        // Update supported services
        if (command.SupportedServices != null && command.SupportedServices.Count > 0)
        {
            var servicesJson = JsonSerializer.Serialize(command.SupportedServices);
            provider.SetSupportedServices(servicesJson);
        }

        // Update sort order
        if (command.SortOrder.HasValue)
        {
            provider.SetSortOrder(command.SortOrder.Value);
        }

        // Update active status
        if (command.IsActive.HasValue)
        {
            if (command.IsActive.Value)
                provider.Activate();
            else
                provider.Deactivate();
        }

        // Update COD support
        if (command.SupportsCod.HasValue)
        {
            provider.SetCodSupport(command.SupportsCod.Value);
        }

        // Update insurance support
        if (command.SupportsInsurance.HasValue)
        {
            provider.SetInsuranceSupport(command.SupportsInsurance.Value);
        }

        // Update API base URL
        if (command.ApiBaseUrl != null)
        {
            provider.SetApiBaseUrl(command.ApiBaseUrl);
        }

        // Update tracking URL template
        if (command.TrackingUrlTemplate != null)
        {
            provider.SetTrackingUrlTemplate(command.TrackingUrlTemplate);
        }

        // Update weight limits
        if (command.MinWeightGrams.HasValue || command.MaxWeightGrams.HasValue)
        {
            provider.SetWeightLimits(
                command.MinWeightGrams ?? provider.MinWeightGrams,
                command.MaxWeightGrams ?? provider.MaxWeightGrams);
        }

        // Update COD limits
        if (command.MinCodAmount.HasValue || command.MaxCodAmount.HasValue)
        {
            provider.SetCodLimits(
                command.MinCodAmount ?? provider.MinCodAmount,
                command.MaxCodAmount ?? provider.MaxCodAmount);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(provider.ToDto());
    }
}
