namespace NOIR.Application.Features.Payments.Commands.UpdateGateway;

/// <summary>
/// Handler for updating a payment gateway configuration.
/// </summary>
public class UpdateGatewayCommandHandler
{
    private readonly IRepository<PaymentGateway, Guid> _gatewayRepository;
    private readonly ICredentialEncryptionService _encryptionService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateGatewayCommandHandler(
        IRepository<PaymentGateway, Guid> gatewayRepository,
        ICredentialEncryptionService encryptionService,
        IUnitOfWork unitOfWork)
    {
        _gatewayRepository = gatewayRepository;
        _encryptionService = encryptionService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PaymentGatewayDto>> Handle(
        UpdateGatewayCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new PaymentGatewayByIdForUpdateSpec(command.GatewayId);
        var gateway = await _gatewayRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (gateway == null)
        {
            return Result.Failure<PaymentGatewayDto>(
                Error.NotFound("Payment gateway not found.", ErrorCodes.Payment.GatewayNotFound));
        }

        // Update display name
        if (!string.IsNullOrEmpty(command.DisplayName))
        {
            gateway.UpdateDisplayName(command.DisplayName);
        }

        // Update environment
        if (command.Environment.HasValue)
        {
            gateway.UpdateEnvironment(command.Environment.Value);
        }

        // Update credentials (encrypt if provided)
        if (command.Credentials?.Any() == true)
        {
            var credentialsJson = JsonSerializer.Serialize(command.Credentials);
            var encryptedCredentials = _encryptionService.Encrypt(credentialsJson);
            gateway.UpdateCredentials(encryptedCredentials);
        }

        // Update sort order
        if (command.SortOrder.HasValue)
        {
            gateway.SetSortOrder(command.SortOrder.Value);
        }

        // Update active status
        if (command.IsActive.HasValue)
        {
            if (command.IsActive.Value)
            {
                gateway.Activate();
            }
            else
            {
                gateway.Deactivate();
            }
        }

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
