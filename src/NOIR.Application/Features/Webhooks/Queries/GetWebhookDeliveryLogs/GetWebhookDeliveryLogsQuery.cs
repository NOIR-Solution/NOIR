namespace NOIR.Application.Features.Webhooks.Queries.GetWebhookDeliveryLogs;

/// <summary>
/// Query to get delivery logs for a specific webhook subscription.
/// </summary>
public sealed record GetWebhookDeliveryLogsQuery(
    Guid SubscriptionId,
    int Page = 1,
    int PageSize = 20,
    WebhookDeliveryStatus? Status = null);
