namespace NOIR.Application.Features.Payments.Queries.GetWebhookLogs;

/// <summary>
/// Query to get webhook logs with filtering and pagination.
/// </summary>
public sealed record GetWebhookLogsQuery(
    string? Provider = null,
    WebhookProcessingStatus? Status = null,
    int Page = 1,
    int PageSize = 20);
