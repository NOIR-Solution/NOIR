namespace NOIR.Application.Features.Payments.Queries.GetWebhookLogs;

/// <summary>
/// Handler for getting webhook logs.
/// </summary>
public class GetWebhookLogsQueryHandler
{
    private readonly IRepository<PaymentWebhookLog, Guid> _webhookLogRepository;

    public GetWebhookLogsQueryHandler(IRepository<PaymentWebhookLog, Guid> webhookLogRepository)
    {
        _webhookLogRepository = webhookLogRepository;
    }

    public async Task<Result<PagedResult<WebhookLogDto>>> Handle(
        GetWebhookLogsQuery query,
        CancellationToken cancellationToken)
    {
        var skip = (query.Page - 1) * query.PageSize;

        var spec = new WebhookLogsSpec(
            query.Provider,
            query.Status,
            skip,
            query.PageSize);

        var logs = await _webhookLogRepository.ListAsync(spec, cancellationToken);

        // Count without pagination
        var countSpec = new WebhookLogsSpec(query.Provider, query.Status);
        var totalCount = await _webhookLogRepository.CountAsync(countSpec, cancellationToken);

        var items = logs.Select(l => new WebhookLogDto(
            l.Id,
            l.PaymentGatewayId,
            l.Provider,
            l.EventType,
            l.GatewayEventId,
            l.SignatureValid,
            l.ProcessingStatus,
            l.ProcessingError,
            l.RetryCount,
            l.PaymentTransactionId,
            l.IpAddress,
            l.CreatedAt)).ToList();

        var result = PagedResult<WebhookLogDto>.Create(
            items,
            totalCount,
            query.Page - 1,  // Convert 1-based page to 0-based pageIndex
            query.PageSize);

        return Result.Success(result);
    }
}
