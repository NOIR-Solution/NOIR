namespace NOIR.Application.Features.Payments.Queries.GetPaymentTimeline;

/// <summary>
/// Handler for getting payment event timeline.
/// </summary>
public class GetPaymentTimelineQueryHandler
{
    private readonly IRepository<PaymentTransaction, Guid> _paymentRepository;
    private readonly IRepository<PaymentOperationLog, Guid> _operationLogRepository;
    private readonly IRepository<PaymentWebhookLog, Guid> _webhookLogRepository;
    private readonly IRepository<Refund, Guid> _refundRepository;

    public GetPaymentTimelineQueryHandler(
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

    public async Task<Result<IReadOnlyList<PaymentTimelineEventDto>>> Handle(
        GetPaymentTimelineQuery query,
        CancellationToken cancellationToken)
    {
        var paymentSpec = new PaymentTransactionByIdSpec(query.PaymentTransactionId);
        var payment = await _paymentRepository.FirstOrDefaultAsync(paymentSpec, cancellationToken);

        if (payment == null)
        {
            return Result.Failure<IReadOnlyList<PaymentTimelineEventDto>>(
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

        var events = new List<PaymentTimelineEventDto>();

        // Add creation event
        events.Add(new PaymentTimelineEventDto(
            payment.CreatedAt,
            "StatusChange",
            $"Payment created - {payment.Amount} {payment.Currency} via {payment.PaymentMethod}",
            null,
            "system"));

        // Add PaidAt event if exists
        if (payment.PaidAt.HasValue)
        {
            events.Add(new PaymentTimelineEventDto(
                payment.PaidAt.Value,
                "StatusChange",
                "Payment completed",
                null,
                "system"));
        }

        // Map operation logs to timeline events
        foreach (var log in operationLogs)
        {
            var successText = log.Success ? "Success" : "Failed";
            var summary = $"{log.OperationType} - {successText}";
            if (!string.IsNullOrEmpty(log.ErrorMessage))
            {
                summary += $": {log.ErrorMessage}";
            }

            var details = BuildOperationLogDetails(log);

            events.Add(new PaymentTimelineEventDto(
                log.CreatedAt,
                "ApiCall",
                summary,
                details,
                log.UserId ?? "system"));
        }

        // Map webhook logs to timeline events
        foreach (var webhook in webhookLogs)
        {
            var summary = $"Webhook: {webhook.EventType}";
            if (!webhook.SignatureValid)
            {
                summary += " (invalid signature)";
            }

            events.Add(new PaymentTimelineEventDto(
                webhook.CreatedAt,
                "Webhook",
                summary,
                webhook.RequestBody,
                "gateway"));
        }

        // Map refunds to timeline events
        foreach (var refund in refunds)
        {
            var summary = $"Refund {refund.Status} - {refund.Amount} {refund.Currency}";
            if (!string.IsNullOrEmpty(refund.ReasonDetail))
            {
                summary += $" ({refund.ReasonDetail})";
            }

            events.Add(new PaymentTimelineEventDto(
                refund.CreatedAt,
                "Refund",
                summary,
                null,
                refund.RequestedBy ?? "system"));
        }

        // Sort by timestamp descending
        var sortedEvents = events
            .OrderByDescending(e => e.Timestamp)
            .ToList();

        return Result.Success<IReadOnlyList<PaymentTimelineEventDto>>(sortedEvents);
    }

    private static string? BuildOperationLogDetails(PaymentOperationLog log)
    {
        if (string.IsNullOrEmpty(log.RequestData) && string.IsNullOrEmpty(log.ResponseData))
            return null;

        var details = new Dictionary<string, string?>();
        if (!string.IsNullOrEmpty(log.RequestData))
            details["request"] = log.RequestData;
        if (!string.IsNullOrEmpty(log.ResponseData))
            details["response"] = log.ResponseData;
        if (log.HttpStatusCode.HasValue)
            details["httpStatusCode"] = log.HttpStatusCode.Value.ToString();
        if (log.DurationMs > 0)
            details["durationMs"] = log.DurationMs.ToString();

        return JsonSerializer.Serialize(details);
    }
}
