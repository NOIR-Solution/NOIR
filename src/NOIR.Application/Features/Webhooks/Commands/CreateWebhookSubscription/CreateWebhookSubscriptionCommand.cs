namespace NOIR.Application.Features.Webhooks.Commands.CreateWebhookSubscription;

/// <summary>
/// Command to create a new webhook subscription.
/// </summary>
public sealed record CreateWebhookSubscriptionCommand(
    string Name,
    string Url,
    string EventPatterns,
    string? Description = null,
    string? CustomHeaders = null,
    int MaxRetries = 5,
    int TimeoutSeconds = 30) : IAuditableCommand<WebhookSubscriptionDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => UserId;
    public string? GetTargetDisplayName() => Name;
    public string? GetActionDescription() => $"Created webhook subscription '{Name}' for {Url}";
}
