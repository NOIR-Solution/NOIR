namespace NOIR.Application.Features.Webhooks.Queries.GetWebhookDeliveryLogs;

/// <summary>
/// Wolverine handler for getting delivery logs for a webhook subscription.
/// Uses IApplicationDbContext directly since WebhookDeliveryLog is a TenantEntity, not an AggregateRoot.
/// </summary>
public class GetWebhookDeliveryLogsQueryHandler
{
    private readonly IApplicationDbContext _dbContext;

    public GetWebhookDeliveryLogsQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PagedResult<WebhookDeliveryLogDto>>> Handle(
        GetWebhookDeliveryLogsQuery query,
        CancellationToken cancellationToken)
    {
        var baseQuery = _dbContext.WebhookDeliveryLogs
            .TagWith("GetWebhookDeliveryLogs")
            .Where(l => l.WebhookSubscriptionId == query.SubscriptionId);

        if (query.Status.HasValue)
            baseQuery = baseQuery.Where(l => l.Status == query.Status.Value);

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var skip = (query.Page - 1) * query.PageSize;
        var logs = await baseQuery
            .OrderByDescending(l => l.CreatedAt)
            .Skip(skip)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var items = logs.Select(WebhookMapper.ToDto).ToList();

        var pageIndex = query.Page - 1;
        return Result.Success(PagedResult<WebhookDeliveryLogDto>.Create(
            items,
            totalCount,
            pageIndex,
            query.PageSize));
    }
}
