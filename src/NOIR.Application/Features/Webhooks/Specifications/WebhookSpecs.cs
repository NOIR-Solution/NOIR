namespace NOIR.Application.Features.Webhooks.Specifications;

/// <summary>
/// Specification to find all active webhook subscriptions that can receive events.
/// </summary>
public sealed class ActiveWebhookSubscriptionsSpec : Specification<WebhookSubscription>
{
    public ActiveWebhookSubscriptionsSpec()
    {
        Query.Where(s => s.IsActive && s.Status == WebhookSubscriptionStatus.Active)
            .TagWith("ActiveWebhookSubscriptions");
    }
}

/// <summary>
/// Specification to get a webhook subscription by ID (read-only).
/// </summary>
public sealed class WebhookSubscriptionByIdSpec : Specification<WebhookSubscription>
{
    public WebhookSubscriptionByIdSpec(Guid subscriptionId)
    {
        Query.Where(s => s.Id == subscriptionId)
            .TagWith("WebhookSubscriptionById");
    }
}

/// <summary>
/// Specification to get a webhook subscription by ID for mutation (with tracking).
/// </summary>
public sealed class WebhookSubscriptionByIdForUpdateSpec : Specification<WebhookSubscription>
{
    public WebhookSubscriptionByIdForUpdateSpec(Guid subscriptionId)
    {
        Query.Where(s => s.Id == subscriptionId)
            .AsTracking()
            .TagWith("WebhookSubscriptionByIdForUpdate");
    }
}

/// <summary>
/// Specification to check for duplicate webhook subscription URL within a tenant.
/// </summary>
public sealed class WebhookSubscriptionByUrlSpec : Specification<WebhookSubscription>
{
    public WebhookSubscriptionByUrlSpec(string url, Guid? excludeId = null)
    {
        Query.Where(s => s.Url == url);

        if (excludeId.HasValue)
            Query.Where(s => s.Id != excludeId.Value);

        Query.TagWith("WebhookSubscriptionByUrl");
    }
}

/// <summary>
/// Specification to filter webhook subscriptions with pagination.
/// </summary>
public sealed class WebhookSubscriptionsFilterSpec : Specification<WebhookSubscription>
{
    public WebhookSubscriptionsFilterSpec(
        int skip = 0,
        int take = 20,
        string? search = null,
        WebhookSubscriptionStatus? status = null)
    {
        Query.TagWith("WebhookSubscriptionsFilter");

        if (!string.IsNullOrEmpty(search))
            Query.Where(s => s.Name.Contains(search) || s.Url.Contains(search));

        if (status.HasValue)
            Query.Where(s => s.Status == status.Value);

        Query.OrderByDescending(s => s.CreatedAt)
            .Skip(skip)
            .Take(take);
    }
}

/// <summary>
/// Specification to count webhook subscriptions matching filter criteria.
/// </summary>
public sealed class WebhookSubscriptionsCountSpec : Specification<WebhookSubscription>
{
    public WebhookSubscriptionsCountSpec(
        string? search = null,
        WebhookSubscriptionStatus? status = null)
    {
        Query.TagWith("WebhookSubscriptionsCount");

        if (!string.IsNullOrEmpty(search))
            Query.Where(s => s.Name.Contains(search) || s.Url.Contains(search));

        if (status.HasValue)
            Query.Where(s => s.Status == status.Value);
    }
}
