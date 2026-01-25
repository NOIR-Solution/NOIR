namespace NOIR.Application.Features.Payments.Commands.ConfigureGateway;

/// <summary>
/// Handler for configuring a payment gateway.
/// </summary>
public class ConfigureGatewayCommandHandler
{
    private readonly IRepository<PaymentGateway, Guid> _gatewayRepository;
    private readonly ICredentialEncryptionService _encryptionService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public ConfigureGatewayCommandHandler(
        IRepository<PaymentGateway, Guid> gatewayRepository,
        ICredentialEncryptionService encryptionService,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _gatewayRepository = gatewayRepository;
        _encryptionService = encryptionService;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<PaymentGatewayDto>> Handle(
        ConfigureGatewayCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        // Check if gateway with this provider already exists for this tenant
        var providerSpec = new PaymentGatewayByProviderSpec(command.Provider);
        var existing = await _gatewayRepository.FirstOrDefaultAsync(providerSpec, cancellationToken);
        if (existing != null)
        {
            return Result.Failure<PaymentGatewayDto>(
                Error.Conflict($"Payment gateway for provider '{command.Provider}' already exists.", ErrorCodes.Payment.GatewayAlreadyExists));
        }

        // Encrypt credentials
        var credentialsJson = JsonSerializer.Serialize(command.Credentials);
        var encryptedCredentials = _encryptionService.Encrypt(credentialsJson);

        // Create gateway
        var gateway = PaymentGateway.Create(
            command.Provider,
            command.DisplayName,
            command.Environment,
            tenantId);

        // Configure credentials
        gateway.Configure(encryptedCredentials, null);
        gateway.SetSortOrder(command.SortOrder);

        if (command.IsActive)
        {
            gateway.Activate();
        }

        await _gatewayRepository.AddAsync(gateway, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(MapToDto(gateway));
    }

    private static PaymentGatewayDto MapToDto(PaymentGateway gateway)
    {
        return new PaymentGatewayDto(
            gateway.Id,
            gateway.Provider,
            gateway.DisplayName,
            gateway.IsActive,
            gateway.SortOrder,
            gateway.Environment,
            !string.IsNullOrEmpty(gateway.EncryptedCredentials),
            gateway.WebhookUrl,
            gateway.MinAmount,
            gateway.MaxAmount,
            gateway.SupportedCurrencies,
            gateway.LastHealthCheck,
            gateway.HealthStatus,
            gateway.CreatedAt,
            gateway.ModifiedAt);
    }
}
