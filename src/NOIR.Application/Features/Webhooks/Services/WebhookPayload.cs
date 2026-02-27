namespace NOIR.Application.Features.Webhooks.Services;

/// <summary>
/// The payload structure sent to webhook subscription endpoints.
/// </summary>
public sealed record WebhookPayload
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string EventType { get; init; } = string.Empty;
    public Guid EventId { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public string ApiVersion { get; init; } = "2026-02-26";
    public object Data { get; init; } = new();
}
