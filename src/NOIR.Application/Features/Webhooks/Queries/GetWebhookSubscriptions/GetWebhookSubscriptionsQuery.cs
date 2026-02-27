namespace NOIR.Application.Features.Webhooks.Queries.GetWebhookSubscriptions;

/// <summary>
/// Query to get webhook subscriptions with pagination and filtering.
/// </summary>
public sealed record GetWebhookSubscriptionsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    WebhookSubscriptionStatus? Status = null);
