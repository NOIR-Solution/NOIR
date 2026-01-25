namespace NOIR.Application.Features.Payments.Commands.CreatePayment;

/// <summary>
/// Handler for creating a new payment transaction.
/// </summary>
public class CreatePaymentCommandHandler
{
    private readonly IRepository<PaymentTransaction, Guid> _paymentRepository;
    private readonly IRepository<PaymentGateway, Guid> _gatewayRepository;
    private readonly IPaymentGatewayFactory _gatewayFactory;
    private readonly IPaymentService _paymentService;
    private readonly IPaymentOperationLogger _operationLogger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IOptions<PaymentSettings> _paymentSettings;

    public CreatePaymentCommandHandler(
        IRepository<PaymentTransaction, Guid> paymentRepository,
        IRepository<PaymentGateway, Guid> gatewayRepository,
        IPaymentGatewayFactory gatewayFactory,
        IPaymentService paymentService,
        IPaymentOperationLogger operationLogger,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IOptions<PaymentSettings> paymentSettings)
    {
        _paymentRepository = paymentRepository;
        _gatewayRepository = gatewayRepository;
        _gatewayFactory = gatewayFactory;
        _paymentService = paymentService;
        _operationLogger = operationLogger;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _paymentSettings = paymentSettings;
    }

    public async Task<Result<PaymentTransactionDto>> Handle(
        CreatePaymentCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        // Check for idempotency if key provided
        if (!string.IsNullOrEmpty(command.IdempotencyKey))
        {
            var idempotencySpec = new PaymentTransactionByIdempotencyKeySpec(command.IdempotencyKey);
            var existingPayment = await _paymentRepository.FirstOrDefaultAsync(idempotencySpec, cancellationToken);
            if (existingPayment != null)
            {
                return Result.Success(MapToDto(existingPayment));
            }
        }

        // Get the gateway
        var gatewaySpec = new PaymentGatewayByProviderSpec(command.Provider);
        var gateway = await _gatewayRepository.FirstOrDefaultAsync(gatewaySpec, cancellationToken);
        if (gateway == null)
        {
            return Result.Failure<PaymentTransactionDto>(
                Error.NotFound($"Payment gateway '{command.Provider}' not found.", ErrorCodes.Payment.GatewayNotFound));
        }

        if (!gateway.IsActive)
        {
            return Result.Failure<PaymentTransactionDto>(
                Error.Validation("Provider", "Payment gateway is not active.", ErrorCodes.Payment.GatewayNotActive));
        }

        // Generate transaction number
        var transactionNumber = _paymentService.GenerateTransactionNumber();
        var idempotencyKey = command.IdempotencyKey ?? Guid.NewGuid().ToString("N");

        // Create payment transaction
        var payment = PaymentTransaction.Create(
            transactionNumber,
            gateway.Id,
            command.Provider,
            command.Amount,
            command.Currency,
            command.PaymentMethod,
            idempotencyKey,
            tenantId);

        // Set order reference
        payment.SetOrderId(command.OrderId);

        // Set expiry
        var expiryMinutes = _paymentSettings.Value.PaymentLinkExpiryMinutes;
        payment.SetExpiresAt(DateTimeOffset.UtcNow.AddMinutes(expiryMinutes));

        // Store metadata
        if (command.Metadata?.Any() == true)
        {
            payment.SetMetadataJson(JsonSerializer.Serialize(command.Metadata));
        }

        // Handle COD separately (no gateway call needed)
        if (command.PaymentMethod == PaymentMethod.COD)
        {
            payment.MarkAsCodPending();
            await _paymentRepository.AddAsync(payment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(MapToDto(payment));
        }

        // Initiate payment with gateway
        var gatewayProvider = _gatewayFactory.GetProvider(command.Provider);
        if (gatewayProvider == null)
        {
            return Result.Failure<PaymentTransactionDto>(
                Error.Failure($"Payment provider '{command.Provider}' is not configured.", ErrorCodes.Payment.ProviderNotConfigured));
        }

        var initiationRequest = new PaymentInitiationRequest(
            payment.Id,
            transactionNumber,
            command.Amount,
            command.Currency,
            command.PaymentMethod,
            command.ReturnUrl ?? string.Empty,
            command.Metadata ?? new Dictionary<string, string>());

        // Log the payment initiation operation
        var operationLogId = await _operationLogger.StartOperationAsync(
            PaymentOperationType.InitiatePayment,
            command.Provider,
            transactionNumber,
            payment.Id,
            cancellationToken: cancellationToken);

        await _operationLogger.SetRequestDataAsync(operationLogId, initiationRequest, cancellationToken);

        PaymentInitiationResult initiationResult;
        try
        {
            initiationResult = await gatewayProvider.InitiatePaymentAsync(initiationRequest, cancellationToken);

            if (!initiationResult.Success)
            {
                await _operationLogger.CompleteFailedAsync(
                    operationLogId,
                    ErrorCodes.Payment.InitiationFailed,
                    initiationResult.ErrorMessage,
                    initiationResult,
                    cancellationToken: cancellationToken);

                payment.MarkAsFailed(initiationResult.ErrorMessage ?? "Payment initiation failed");
                await _paymentRepository.AddAsync(payment, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result.Failure<PaymentTransactionDto>(
                    Error.Failure(initiationResult.ErrorMessage ?? "Payment initiation failed", ErrorCodes.Payment.InitiationFailed));
            }

            await _operationLogger.CompleteSuccessAsync(operationLogId, initiationResult, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            await _operationLogger.CompleteFailedAsync(
                operationLogId,
                ErrorCodes.Payment.GatewayError,
                ex.Message,
                exception: ex,
                cancellationToken: cancellationToken);
            throw;
        }

        // Update payment with gateway response
        if (!string.IsNullOrEmpty(initiationResult.GatewayTransactionId))
        {
            payment.SetGatewayTransactionId(initiationResult.GatewayTransactionId);
        }

        if (initiationResult.RequiresAction)
        {
            payment.MarkAsRequiresAction();
        }
        else
        {
            payment.MarkAsProcessing();
        }

        await _paymentRepository.AddAsync(payment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(MapToDto(payment));
    }

    private static PaymentTransactionDto MapToDto(PaymentTransaction payment)
    {
        return new PaymentTransactionDto(
            payment.Id,
            payment.TransactionNumber,
            payment.GatewayTransactionId,
            payment.PaymentGatewayId,
            payment.Provider,
            payment.OrderId,
            payment.CustomerId,
            payment.Amount,
            payment.Currency,
            payment.GatewayFee,
            payment.NetAmount,
            payment.Status,
            payment.FailureReason,
            payment.PaymentMethod,
            payment.PaymentMethodDetail,
            payment.PaidAt,
            payment.ExpiresAt,
            payment.CodCollectorName,
            payment.CodCollectedAt,
            payment.CreatedAt,
            payment.ModifiedAt);
    }
}
