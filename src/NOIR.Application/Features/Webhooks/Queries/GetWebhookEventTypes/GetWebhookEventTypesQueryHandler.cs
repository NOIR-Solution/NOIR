namespace NOIR.Application.Features.Webhooks.Queries.GetWebhookEventTypes;

/// <summary>
/// Wolverine handler for getting all registered webhook event types.
/// </summary>
public class GetWebhookEventTypesQueryHandler
{
    private readonly WebhookEventTypeRegistry _registry;

    public GetWebhookEventTypesQueryHandler(WebhookEventTypeRegistry registry)
    {
        _registry = registry;
    }

    public Task<Result<IReadOnlyList<WebhookEventTypeDto>>> Handle(
        GetWebhookEventTypesQuery query,
        CancellationToken cancellationToken)
    {
        var eventTypes = _registry.GetAllEventTypes();
        return Task.FromResult(Result.Success(eventTypes));
    }
}
