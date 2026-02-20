namespace NOIR.Application.Features.Payments.Queries.GetPaymentDetails;

/// <summary>
/// Handler for getting comprehensive payment details.
/// </summary>
public class GetPaymentDetailsQueryHandler
{
    private readonly IRepository<PaymentTransaction, Guid> _paymentRepository;
    private readonly IRepository<PaymentOperationLog, Guid> _operationLogRepository;
    private readonly IRepository<PaymentWebhookLog, Guid> _webhookLogRepository;
    private readonly IRepository<Refund, Guid> _refundRepository;

    public GetPaymentDetailsQueryHandler(
        IRepository<PaymentTransaction, Guid> paymentRepository,
        IRepository<PaymentOperationLog, Guid> operationLogRepository,
        IRepository<PaymentWebhookLog, Guid> webhookLogRepository,
        IRepository<Refund, Guid> refundRepository)
    {
        _paymentRepository = paymentRepository;
        _operationLogRepository = operationLogRepository;
        _webhookLogRepository = webhookLogRepository;
        _refundRepository = refundRepository;
    }

    public async Task<Result<PaymentDetailsDto>> Handle(
        GetPaymentDetailsQuery query,
        CancellationToken cancellationToken)
    {
        var paymentSpec = new PaymentTransactionByIdSpec(query.PaymentTransactionId);
        var payment = await _paymentRepository.FirstOrDefaultAsync(paymentSpec, cancellationToken);

        if (payment == null)
        {
            return Result.Failure<PaymentDetailsDto>(
                Error.NotFound("Payment transaction not found.", ErrorCodes.Payment.TransactionNotFound));
        }

        // Load all related data in parallel
        var operationLogsSpec = new PaymentOperationLogsByTransactionIdSpec(query.PaymentTransactionId);
        var webhookLogsSpec = new WebhookLogsByPaymentSpec(query.PaymentTransactionId);
        var refundsSpec = new RefundsByPaymentSpec(query.PaymentTransactionId);

        var operationLogsTask = _operationLogRepository.ListAsync(operationLogsSpec, cancellationToken);
        var webhookLogsTask = _webhookLogRepository.ListAsync(webhookLogsSpec, cancellationToken);
        var refundsTask = _refundRepository.ListAsync(refundsSpec, cancellationToken);

        await Task.WhenAll(operationLogsTask, webhookLogsTask, refundsTask);

        var operationLogs = operationLogsTask.Result;
        var webhookLogs = webhookLogsTask.Result;
        var refunds = refundsTask.Result;

        var transactionDto = new PaymentTransactionDto(
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

        var operationLogDtos = operationLogs.Select(l => new PaymentOperationLogDto(
            l.Id,
            l.OperationType,
            l.Provider,
            l.PaymentTransactionId,
            l.TransactionNumber,
            l.RefundId,
            l.CorrelationId,
            l.RequestData,
            l.ResponseData,
            l.HttpStatusCode,
            l.DurationMs,
            l.Success,
            l.ErrorCode,
            l.ErrorMessage,
            l.UserId,
            l.IpAddress,
            l.CreatedAt)).ToList();

        var webhookLogDtos = webhookLogs.Select(w => new WebhookLogDto(
            w.Id,
            w.PaymentGatewayId,
            w.Provider,
            w.EventType,
            w.GatewayEventId,
            w.SignatureValid,
            w.ProcessingStatus,
            w.ProcessingError,
            w.RetryCount,
            w.PaymentTransactionId,
            w.IpAddress,
            w.CreatedAt)).ToList();

        var refundDtos = refunds.Select(r => new RefundDto(
            r.Id,
            r.RefundNumber,
            r.PaymentTransactionId,
            r.GatewayRefundId,
            r.Amount,
            r.Currency,
            r.Status,
            r.Reason,
            r.ReasonDetail,
            r.RequestedBy,
            r.ApprovedBy,
            r.ProcessedAt,
            r.CreatedAt)).ToList();

        var result = new PaymentDetailsDto(
            transactionDto,
            operationLogDtos,
            webhookLogDtos,
            refundDtos);

        return Result.Success(result);
    }
}
