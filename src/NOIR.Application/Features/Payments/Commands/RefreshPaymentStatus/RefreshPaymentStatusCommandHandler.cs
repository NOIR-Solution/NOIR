namespace NOIR.Application.Features.Payments.Commands.RefreshPaymentStatus;

/// <summary>
/// Handler for refreshing payment status from the gateway.
/// </summary>
public class RefreshPaymentStatusCommandHandler
{
    private readonly IRepository<PaymentTransaction, Guid> _paymentRepository;
    private readonly IPaymentGatewayFactory _gatewayFactory;
    private readonly IPaymentOperationLogger _operationLogger;
    private readonly IUnitOfWork _unitOfWork;

    private static readonly PaymentStatus[] TerminalStatuses =
    [
        PaymentStatus.Paid,
        PaymentStatus.Failed,
        PaymentStatus.Cancelled,
        PaymentStatus.Expired,
        PaymentStatus.Refunded
    ];

    public RefreshPaymentStatusCommandHandler(
        IRepository<PaymentTransaction, Guid> paymentRepository,
        IPaymentGatewayFactory gatewayFactory,
        IPaymentOperationLogger operationLogger,
        IUnitOfWork unitOfWork)
    {
        _paymentRepository = paymentRepository;
        _gatewayFactory = gatewayFactory;
        _operationLogger = operationLogger;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PaymentTransactionDto>> Handle(
        RefreshPaymentStatusCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new PaymentTransactionByIdForUpdateSpec(command.PaymentTransactionId);
        var payment = await _paymentRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (payment == null)
        {
            return Result.Failure<PaymentTransactionDto>(
                Error.NotFound("Payment transaction not found.", ErrorCodes.Payment.TransactionNotFound));
        }

        if (TerminalStatuses.Contains(payment.Status))
        {
            return Result.Failure<PaymentTransactionDto>(
                Error.Validation("PaymentTransactionId", "Cannot refresh payment in terminal state.", ErrorCodes.Payment.InvalidStatusTransition));
        }

        if (string.IsNullOrEmpty(payment.GatewayTransactionId))
        {
            return Result.Failure<PaymentTransactionDto>(
                Error.Validation("PaymentTransactionId", "Payment has no gateway transaction ID. Manual or COD payments cannot be refreshed.", ErrorCodes.Payment.NoGatewayTransaction));
        }

        var provider = await _gatewayFactory.GetProviderWithCredentialsAsync(payment.Provider, cancellationToken);
        if (provider == null)
        {
            return Result.Failure<PaymentTransactionDto>(
                Error.NotFound($"Payment provider '{payment.Provider}' is not configured.", ErrorCodes.Payment.ProviderNotConfigured));
        }

        var operationLogId = await _operationLogger.StartOperationAsync(
            PaymentOperationType.ManualRefresh,
            payment.Provider,
            payment.TransactionNumber,
            payment.Id,
            cancellationToken: cancellationToken);

        await _operationLogger.SetRequestDataAsync(operationLogId, new { payment.GatewayTransactionId }, cancellationToken);

        PaymentStatusResult statusResult;
        try
        {
            statusResult = await provider.GetPaymentStatusAsync(payment.GatewayTransactionId, cancellationToken);
        }
        catch (Exception ex)
        {
            await _operationLogger.CompleteFailedAsync(
                operationLogId,
                ErrorCodes.Payment.GatewayError,
                ex.Message,
                exception: ex,
                cancellationToken: cancellationToken);

            return Result.Failure<PaymentTransactionDto>(
                Error.Failure(ErrorCodes.Payment.GatewayError, "Unable to communicate with the payment gateway. Please try again later."));
        }

        if (!statusResult.Success)
        {
            await _operationLogger.CompleteFailedAsync(
                operationLogId,
                ErrorCodes.Payment.GatewayError,
                statusResult.ErrorMessage,
                statusResult,
                cancellationToken: cancellationToken);

            return Result.Failure<PaymentTransactionDto>(
                Error.Failure(ErrorCodes.Payment.GatewayError, statusResult.ErrorMessage ?? "Failed to get payment status from gateway."));
        }

        // Update payment based on gateway status
        if (statusResult.Status != payment.Status)
        {
            switch (statusResult.Status)
            {
                case PaymentStatus.Paid:
                    payment.MarkAsPaid(statusResult.GatewayTransactionId ?? payment.GatewayTransactionId);
                    break;
                case PaymentStatus.Failed:
                    payment.MarkAsFailed(statusResult.ErrorMessage ?? "Payment failed at gateway");
                    break;
                case PaymentStatus.Cancelled:
                    payment.MarkAsCancelled();
                    break;
                case PaymentStatus.Expired:
                    payment.MarkAsExpired();
                    break;
                case PaymentStatus.Processing:
                    payment.MarkAsProcessing();
                    break;
                case PaymentStatus.Authorized:
                    payment.MarkAsAuthorized();
                    break;
            }
        }

        if (statusResult.AdditionalData != null)
        {
            payment.SetGatewayResponse(JsonSerializer.Serialize(statusResult.AdditionalData));
        }

        await _operationLogger.CompleteSuccessAsync(operationLogId, statusResult, cancellationToken: cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<PaymentTransactionDto>(
                Error.Conflict("This payment was modified by another user. Please refresh and try again.", ErrorCodes.Payment.ConcurrencyConflict));
        }

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
