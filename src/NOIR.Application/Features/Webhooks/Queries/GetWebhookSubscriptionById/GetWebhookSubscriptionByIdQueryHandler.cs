namespace NOIR.Application.Features.Webhooks.Queries.GetWebhookSubscriptionById;

/// <summary>
/// Wolverine handler for getting a webhook subscription by ID with delivery stats.
/// </summary>
public class GetWebhookSubscriptionByIdQueryHandler
{
    private readonly IRepository<WebhookSubscription, Guid> _repository;
    private readonly IApplicationDbContext _dbContext;

    public GetWebhookSubscriptionByIdQueryHandler(
        IRepository<WebhookSubscription, Guid> repository,
        IApplicationDbContext dbContext)
    {
        _repository = repository;
        _dbContext = dbContext;
    }

    public async Task<Result<WebhookSubscriptionDto>> Handle(
        GetWebhookSubscriptionByIdQuery query,
        CancellationToken cancellationToken)
    {
        var spec = new Webhooks.Specifications.WebhookSubscriptionByIdSpec(query.Id);
        var subscription = await _repository.FirstOrDefaultAsync(spec, cancellationToken);

        if (subscription is null)
        {
            return Result.Failure<WebhookSubscriptionDto>(
                Error.NotFound($"Webhook subscription with ID '{query.Id}' not found.", "NOIR-WEBHOOK-002"));
        }

        // Compute delivery stats via a grouped DB query to avoid loading all delivery logs
        var deliveryStats = await _dbContext.WebhookDeliveryLogs
            .Where(log => log.WebhookSubscriptionId == query.Id)
            .GroupBy(log => log.WebhookSubscriptionId)
            .Select(g => new
            {
                Total = g.Count(),
                Succeeded = g.Count(l => l.Status == WebhookDeliveryStatus.Succeeded),
                Failed = g.Count(l => l.Status == WebhookDeliveryStatus.Failed || l.Status == WebhookDeliveryStatus.Exhausted)
            })
            .TagWith("GetWebhookSubscriptionById_DeliveryStats")
            .FirstOrDefaultAsync(cancellationToken);

        return Result.Success(WebhookMapper.ToDto(
            subscription,
            deliveryStats?.Total ?? 0,
            deliveryStats?.Succeeded ?? 0,
            deliveryStats?.Failed ?? 0));
    }
}
