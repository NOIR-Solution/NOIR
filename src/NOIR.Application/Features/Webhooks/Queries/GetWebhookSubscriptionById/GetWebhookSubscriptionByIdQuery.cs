namespace NOIR.Application.Features.Webhooks.Queries.GetWebhookSubscriptionById;

/// <summary>
/// Query to get a webhook subscription by ID with full details including delivery statistics.
/// </summary>
public sealed record GetWebhookSubscriptionByIdQuery(Guid Id);
