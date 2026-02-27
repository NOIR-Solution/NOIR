namespace NOIR.Application.Features.Webhooks.Services;

/// <summary>
/// Dispatches domain events to matching webhook subscriptions.
/// Finds active subscriptions, creates delivery logs, and publishes
/// DeliverWebhookCommand messages for each matching subscription.
/// </summary>
public sealed class WebhookDispatcher : IWebhookDispatcher, IScopedService
{
    private static readonly JsonSerializerOptions PayloadSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly IRepository<WebhookSubscription, Guid> _repository;
    private readonly WebhookEventTypeRegistry _registry;
    private readonly IApplicationDbContext _dbContext;
    private readonly IMessageBus _messageBus;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<WebhookDispatcher> _logger;

    public WebhookDispatcher(
        IRepository<WebhookSubscription, Guid> repository,
        WebhookEventTypeRegistry registry,
        IApplicationDbContext dbContext,
        IMessageBus messageBus,
        ICurrentUser currentUser,
        ILogger<WebhookDispatcher> logger)
    {
        _repository = repository;
        _registry = registry;
        _dbContext = dbContext;
        _messageBus = messageBus;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken ct = default)
    {
        var eventType = _registry.GetEventType(domainEvent);
        if (eventType is null)
        {
            _logger.LogDebug("Domain event {EventType} is not webhook-eligible, skipping", domainEvent.GetType().Name);
            return;
        }

        // Query all active webhook subscriptions for the current tenant
        var spec = new ActiveWebhookSubscriptionsSpec();
        var subscriptions = await _repository.ListAsync(spec, ct);

        if (subscriptions.Count == 0)
        {
            _logger.LogDebug("No active webhook subscriptions found for event {EventType}", eventType);
            return;
        }

        // Filter subscriptions that match this event type
        var matchingSubscriptions = subscriptions.Where(s => s.MatchesEvent(eventType)).ToList();

        if (matchingSubscriptions.Count == 0)
        {
            _logger.LogDebug("No webhook subscriptions match event {EventType}", eventType);
            return;
        }

        _logger.LogInformation(
            "Dispatching webhook event {EventType} to {Count} subscription(s)",
            eventType, matchingSubscriptions.Count);

        // Build the payload once for all subscriptions
        var payload = new WebhookPayload
        {
            EventType = eventType,
            EventId = domainEvent.EventId,
            Timestamp = domainEvent.OccurredAt,
            Data = domainEvent
        };

        var payloadJson = JsonSerializer.Serialize(payload, PayloadSerializerOptions);

        // Create delivery log entries for all matching subscriptions
        var deliveryCommands = new List<Commands.DeliverWebhook.DeliverWebhookCommand>();

        foreach (var subscription in matchingSubscriptions)
        {
            var deliveryLog = WebhookDeliveryLog.Create(
                subscription.Id,
                eventType,
                domainEvent.EventId,
                subscription.Url,
                payloadJson,
                subscription.CustomHeaders,
                _currentUser.TenantId);

            _dbContext.WebhookDeliveryLogs.Add(deliveryLog);

            deliveryCommands.Add(new Commands.DeliverWebhook.DeliverWebhookCommand(
                subscription.Id,
                deliveryLog.Id,
                eventType,
                domainEvent.EventId,
                payloadJson,
                subscription.Secret,
                subscription.Url,
                subscription.TimeoutSeconds,
                subscription.MaxRetries,
                1,
                subscription.CustomHeaders));
        }

        // Persist delivery logs BEFORE publishing commands to avoid race condition
        await _dbContext.SaveChangesAsync(ct);

        // Publish delivery commands via Wolverine message bus
        foreach (var deliverCommand in deliveryCommands)
        {
            await _messageBus.PublishAsync(deliverCommand);

            _logger.LogDebug(
                "Published DeliverWebhookCommand for subscription {SubscriptionId} / delivery {DeliveryLogId}",
                deliverCommand.SubscriptionId, deliverCommand.DeliveryLogId);
        }
    }
}
