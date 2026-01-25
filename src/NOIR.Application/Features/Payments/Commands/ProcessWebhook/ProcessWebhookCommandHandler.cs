namespace NOIR.Application.Features.Payments.Commands.ProcessWebhook;

/// <summary>
/// Handler for processing payment gateway webhooks.
/// </summary>
public class ProcessWebhookCommandHandler
{
    private readonly IRepository<PaymentWebhookLog, Guid> _webhookLogRepository;
    private readonly IRepository<PaymentTransaction, Guid> _paymentRepository;
    private readonly IRepository<PaymentGateway, Guid> _gatewayRepository;
    private readonly IPaymentGatewayFactory _gatewayFactory;
    private readonly IPaymentOperationLogger _operationLogger;
    private readonly IUnitOfWork _unitOfWork;

    public ProcessWebhookCommandHandler(
        IRepository<PaymentWebhookLog, Guid> webhookLogRepository,
        IRepository<PaymentTransaction, Guid> paymentRepository,
        IRepository<PaymentGateway, Guid> gatewayRepository,
        IPaymentGatewayFactory gatewayFactory,
        IPaymentOperationLogger operationLogger,
        IUnitOfWork unitOfWork)
    {
        _webhookLogRepository = webhookLogRepository;
        _paymentRepository = paymentRepository;
        _gatewayRepository = gatewayRepository;
        _gatewayFactory = gatewayFactory;
        _operationLogger = operationLogger;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<WebhookLogDto>> Handle(
        ProcessWebhookCommand command,
        CancellationToken cancellationToken)
    {
        // Get the gateway provider
        var gatewayProvider = _gatewayFactory.GetProvider(command.Provider);
        if (gatewayProvider == null)
        {
            return Result.Failure<WebhookLogDto>(
                Error.NotFound($"Payment provider '{command.Provider}' is not configured.", ErrorCodes.Payment.ProviderNotConfigured));
        }

        // Get the gateway entity
        var gatewaySpec = new PaymentGatewayByProviderSpec(command.Provider);
        var gateway = await _gatewayRepository.FirstOrDefaultAsync(gatewaySpec, cancellationToken);
        if (gateway == null)
        {
            return Result.Failure<WebhookLogDto>(
                Error.NotFound($"Payment gateway '{command.Provider}' not found.", ErrorCodes.Payment.GatewayNotFound));
        }

        // Log and validate webhook signature
        var operationLogId = await _operationLogger.StartOperationAsync(
            PaymentOperationType.ValidateWebhook,
            command.Provider,
            cancellationToken: cancellationToken);

        var webhookPayload = new WebhookPayload(command.RawPayload, command.Signature, command.Headers ?? new Dictionary<string, string>());
        await _operationLogger.SetRequestDataAsync(operationLogId, new { EventType = "webhook", Provider = command.Provider, HasSignature = !string.IsNullOrEmpty(command.Signature) }, cancellationToken);

        WebhookValidationResult validationResult;
        try
        {
            validationResult = await gatewayProvider.ValidateWebhookAsync(webhookPayload, cancellationToken);

            if (validationResult.IsValid)
            {
                await _operationLogger.CompleteSuccessAsync(operationLogId, validationResult, cancellationToken: cancellationToken);
            }
            else
            {
                await _operationLogger.CompleteFailedAsync(
                    operationLogId,
                    ErrorCodes.Payment.InvalidWebhookSignature,
                    "Webhook signature validation failed",
                    validationResult,
                    cancellationToken: cancellationToken);
            }
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

        // Parse the webhook payload
        var eventType = validationResult.EventType ?? "unknown";
        var gatewayEventId = validationResult.GatewayEventId;
        var gatewayTransactionId = validationResult.GatewayTransactionId;

        // Check for duplicate webhook (by gateway event ID)
        if (!string.IsNullOrEmpty(gatewayEventId))
        {
            var deduplicationSpec = new WebhookLogByGatewayEventIdSpec(gatewayEventId);
            var existing = await _webhookLogRepository.FirstOrDefaultAsync(deduplicationSpec, cancellationToken);
            if (existing != null)
            {
                return Result.Success(MapToDto(existing));
            }
        }

        // Create webhook log
        var webhookLog = PaymentWebhookLog.Create(
            gateway.Id,
            command.Provider,
            eventType,
            command.RawPayload,
            gateway.TenantId);

        webhookLog.SetGatewayEventId(gatewayEventId ?? string.Empty);
        webhookLog.SetRequestDetails(
            command.Headers != null ? JsonSerializer.Serialize(command.Headers) : null,
            command.Signature,
            command.IpAddress);
        webhookLog.MarkSignatureValid(validationResult.IsValid);

        if (!validationResult.IsValid)
        {
            webhookLog.MarkAsFailed("Invalid webhook signature");
            await _webhookLogRepository.AddAsync(webhookLog, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Failure<WebhookLogDto>(
                Error.Validation("Signature", "Invalid webhook signature.", ErrorCodes.Payment.InvalidWebhookSignature));
        }

        // Find the related payment transaction
        PaymentTransaction? payment = null;
        if (!string.IsNullOrEmpty(gatewayTransactionId))
        {
            var paymentSpec = new PaymentTransactionByGatewayTransactionIdSpec(gatewayTransactionId);
            payment = await _paymentRepository.FirstOrDefaultAsync(paymentSpec, cancellationToken);
        }

        if (payment != null)
        {
            // Update payment status based on webhook event
            ProcessPaymentStatusUpdate(payment, validationResult);
        }

        webhookLog.MarkAsProcessed(payment?.Id);
        await _webhookLogRepository.AddAsync(webhookLog, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(MapToDto(webhookLog));
    }

    private static void ProcessPaymentStatusUpdate(PaymentTransaction payment, WebhookValidationResult validationResult)
    {
        switch (validationResult.PaymentStatus)
        {
            case PaymentStatus.Paid:
                payment.MarkAsPaid(validationResult.GatewayTransactionId ?? string.Empty);
                break;
            case PaymentStatus.Failed:
                payment.MarkAsFailed(validationResult.ErrorMessage ?? "Payment failed via webhook");
                break;
            case PaymentStatus.Cancelled:
                payment.MarkAsCancelled();
                break;
            case PaymentStatus.Authorized:
                payment.MarkAsAuthorized();
                break;
            case PaymentStatus.Refunded:
                payment.MarkAsRefunded();
                break;
        }
    }

    private static WebhookLogDto MapToDto(PaymentWebhookLog log)
    {
        return new WebhookLogDto(
            log.Id,
            log.PaymentGatewayId,
            log.Provider,
            log.EventType,
            log.GatewayEventId,
            log.SignatureValid,
            log.ProcessingStatus,
            log.ProcessingError,
            log.RetryCount,
            log.PaymentTransactionId,
            log.IpAddress,
            log.CreatedAt);
    }
}
