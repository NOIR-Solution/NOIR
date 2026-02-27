namespace NOIR.Application.Features.Webhooks.DTOs;

/// <summary>
/// Mapper for Webhook-related entities to DTOs.
/// </summary>
public static class WebhookMapper
{
    /// <summary>
    /// Maps a WebhookSubscription entity to a full DTO including delivery statistics.
    /// </summary>
    public static WebhookSubscriptionDto ToDto(WebhookSubscription subscription)
    {
        var deliveryLogs = subscription.DeliveryLogs;

        return new WebhookSubscriptionDto
        {
            Id = subscription.Id,
            Name = subscription.Name,
            Url = subscription.Url,
            EventPatterns = subscription.EventPatterns,
            Description = subscription.Description,
            CustomHeaders = subscription.CustomHeaders,
            IsActive = subscription.IsActive,
            MaxRetries = subscription.MaxRetries,
            TimeoutSeconds = subscription.TimeoutSeconds,
            Status = subscription.Status,
            LastDeliveryAt = subscription.LastDeliveryAt,
            CreatedAt = subscription.CreatedAt,
            ModifiedAt = subscription.ModifiedAt,
            TotalDeliveries = deliveryLogs.Count,
            SuccessfulDeliveries = deliveryLogs.Count(l => l.Status == WebhookDeliveryStatus.Succeeded),
            FailedDeliveries = deliveryLogs.Count(l => l.Status is WebhookDeliveryStatus.Failed or WebhookDeliveryStatus.Exhausted)
        };
    }

    /// <summary>
    /// Maps a WebhookSubscription entity to a full DTO with pre-computed delivery statistics.
    /// </summary>
    public static WebhookSubscriptionDto ToDto(
        WebhookSubscription subscription,
        int totalDeliveries,
        int successfulDeliveries,
        int failedDeliveries)
    {
        return new WebhookSubscriptionDto
        {
            Id = subscription.Id,
            Name = subscription.Name,
            Url = subscription.Url,
            EventPatterns = subscription.EventPatterns,
            Description = subscription.Description,
            CustomHeaders = subscription.CustomHeaders,
            IsActive = subscription.IsActive,
            MaxRetries = subscription.MaxRetries,
            TimeoutSeconds = subscription.TimeoutSeconds,
            Status = subscription.Status,
            LastDeliveryAt = subscription.LastDeliveryAt,
            CreatedAt = subscription.CreatedAt,
            ModifiedAt = subscription.ModifiedAt,
            TotalDeliveries = totalDeliveries,
            SuccessfulDeliveries = successfulDeliveries,
            FailedDeliveries = failedDeliveries
        };
    }

    /// <summary>
    /// Maps a WebhookSubscription entity to a summary DTO for list views.
    /// </summary>
    public static WebhookSubscriptionSummaryDto ToSummaryDto(WebhookSubscription subscription)
    {
        return new WebhookSubscriptionSummaryDto
        {
            Id = subscription.Id,
            Name = subscription.Name,
            Url = subscription.Url,
            EventPatterns = subscription.EventPatterns,
            IsActive = subscription.IsActive,
            Status = subscription.Status,
            LastDeliveryAt = subscription.LastDeliveryAt
        };
    }

    /// <summary>
    /// Maps a WebhookSubscription entity to a summary DTO with pre-computed delivery statistics.
    /// </summary>
    public static WebhookSubscriptionSummaryDto ToSummaryDto(
        WebhookSubscription subscription,
        int totalDeliveries,
        int successfulDeliveries,
        int failedDeliveries)
    {
        return new WebhookSubscriptionSummaryDto
        {
            Id = subscription.Id,
            Name = subscription.Name,
            Url = subscription.Url,
            EventPatterns = subscription.EventPatterns,
            IsActive = subscription.IsActive,
            Status = subscription.Status,
            LastDeliveryAt = subscription.LastDeliveryAt,
            TotalDeliveries = totalDeliveries,
            SuccessfulDeliveries = successfulDeliveries,
            FailedDeliveries = failedDeliveries
        };
    }

    /// <summary>
    /// Maps a WebhookDeliveryLog entity to a DTO.
    /// </summary>
    public static WebhookDeliveryLogDto ToDto(WebhookDeliveryLog log)
    {
        return new WebhookDeliveryLogDto
        {
            Id = log.Id,
            WebhookSubscriptionId = log.WebhookSubscriptionId,
            EventType = log.EventType,
            EventId = log.EventId,
            RequestUrl = log.RequestUrl,
            ResponseStatusCode = log.ResponseStatusCode,
            Status = log.Status,
            AttemptNumber = log.AttemptNumber,
            NextRetryAt = log.NextRetryAt,
            ErrorMessage = log.ErrorMessage,
            DurationMs = log.DurationMs,
            CreatedAt = log.CreatedAt
        };
    }
}
