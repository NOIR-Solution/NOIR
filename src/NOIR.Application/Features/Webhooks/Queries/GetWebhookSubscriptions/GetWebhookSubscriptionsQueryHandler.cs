namespace NOIR.Application.Features.Webhooks.Queries.GetWebhookSubscriptions;

/// <summary>
/// Wolverine handler for getting webhook subscriptions with pagination.
/// </summary>
public class GetWebhookSubscriptionsQueryHandler
{
    private readonly IRepository<WebhookSubscription, Guid> _repository;
    private readonly IApplicationDbContext _dbContext;

    public GetWebhookSubscriptionsQueryHandler(
        IRepository<WebhookSubscription, Guid> repository,
        IApplicationDbContext dbContext)
    {
        _repository = repository;
        _dbContext = dbContext;
    }

    public async Task<Result<PagedResult<WebhookSubscriptionSummaryDto>>> Handle(
        GetWebhookSubscriptionsQuery query,
        CancellationToken cancellationToken)
    {
        var skip = (query.Page - 1) * query.PageSize;

        // Get total count
        var countSpec = new Webhooks.Specifications.WebhookSubscriptionsCountSpec(
            query.Search,
            query.Status);
        var totalCount = await _repository.CountAsync(countSpec, cancellationToken);

        // Get subscriptions
        var listSpec = new Webhooks.Specifications.WebhookSubscriptionsFilterSpec(
            skip,
            query.PageSize,
            query.Search,
            query.Status);
        var subscriptions = await _repository.ListAsync(listSpec, cancellationToken);

        // Compute delivery stats in a single grouped query to avoid N+1
        var subscriptionIds = subscriptions.Select(s => s.Id).ToList();
        var deliveryStats = await _dbContext.WebhookDeliveryLogs
            .Where(log => subscriptionIds.Contains(log.WebhookSubscriptionId))
            .GroupBy(log => log.WebhookSubscriptionId)
            .Select(g => new
            {
                SubscriptionId = g.Key,
                Total = g.Count(),
                Succeeded = g.Count(l => l.Status == WebhookDeliveryStatus.Succeeded),
                Failed = g.Count(l => l.Status == WebhookDeliveryStatus.Failed || l.Status == WebhookDeliveryStatus.Exhausted)
            })
            .TagWith("GetWebhookSubscriptions_DeliveryStats")
            .ToDictionaryAsync(x => x.SubscriptionId, cancellationToken);

        var items = subscriptions.Select(s =>
        {
            var stats = deliveryStats.GetValueOrDefault(s.Id);
            return WebhookMapper.ToSummaryDto(
                s,
                stats?.Total ?? 0,
                stats?.Succeeded ?? 0,
                stats?.Failed ?? 0);
        }).ToList();

        var pageIndex = query.Page - 1;
        return Result.Success(PagedResult<WebhookSubscriptionSummaryDto>.Create(
            items,
            totalCount,
            pageIndex,
            query.PageSize));
    }
}
