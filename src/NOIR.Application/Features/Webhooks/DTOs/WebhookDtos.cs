namespace NOIR.Application.Features.Webhooks.DTOs;

/// <summary>
/// Full DTO for WebhookSubscription including delivery statistics.
/// </summary>
public sealed record WebhookSubscriptionDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string EventPatterns { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? CustomHeaders { get; init; }
    public bool IsActive { get; init; }
    public int MaxRetries { get; init; }
    public int TimeoutSeconds { get; init; }
    public WebhookSubscriptionStatus Status { get; init; }
    public DateTimeOffset? LastDeliveryAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ModifiedAt { get; init; }
    public int TotalDeliveries { get; init; }
    public int SuccessfulDeliveries { get; init; }
    public int FailedDeliveries { get; init; }
}

/// <summary>
/// Summary DTO for WebhookSubscription list views.
/// </summary>
public sealed record WebhookSubscriptionSummaryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string EventPatterns { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public WebhookSubscriptionStatus Status { get; init; }
    public DateTimeOffset? LastDeliveryAt { get; init; }
    public int TotalDeliveries { get; init; }
    public int SuccessfulDeliveries { get; init; }
    public int FailedDeliveries { get; init; }
}

/// <summary>
/// DTO for WebhookDeliveryLog entries.
/// </summary>
public sealed record WebhookDeliveryLogDto
{
    public Guid Id { get; init; }
    public Guid WebhookSubscriptionId { get; init; }
    public string EventType { get; init; } = string.Empty;
    public Guid EventId { get; init; }
    public string RequestUrl { get; init; } = string.Empty;
    public int? ResponseStatusCode { get; init; }
    public WebhookDeliveryStatus Status { get; init; }
    public int AttemptNumber { get; init; }
    public DateTimeOffset? NextRetryAt { get; init; }
    public string? ErrorMessage { get; init; }
    public long? DurationMs { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// DTO for a registered webhook event type.
/// </summary>
public sealed record WebhookEventTypeDto
{
    public string EventType { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}

/// <summary>
/// DTO for returning a webhook subscription secret after rotation.
/// </summary>
public sealed record WebhookSecretDto
{
    public string Secret { get; init; } = string.Empty;
}
