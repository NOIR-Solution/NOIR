namespace NOIR.Application.Features.Webhooks.Commands.TestWebhookSubscription;

/// <summary>
/// Wolverine handler for sending a test ping to a webhook subscription.
/// Creates a test payload and publishes a delivery command.
/// </summary>
public class TestWebhookSubscriptionCommandHandler
{
    private readonly IRepository<WebhookSubscription, Guid> _repository;
    private readonly IApplicationDbContext _dbContext;
    private readonly IMessageBus _messageBus;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<TestWebhookSubscriptionCommandHandler> _logger;

    public TestWebhookSubscriptionCommandHandler(
        IRepository<WebhookSubscription, Guid> repository,
        IApplicationDbContext dbContext,
        IMessageBus messageBus,
        ICurrentUser currentUser,
        ILogger<TestWebhookSubscriptionCommandHandler> logger)
    {
        _repository = repository;
        _dbContext = dbContext;
        _messageBus = messageBus;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<WebhookDeliveryLogDto>> Handle(
        TestWebhookSubscriptionCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new Webhooks.Specifications.WebhookSubscriptionByIdForUpdateSpec(command.Id);
        var subscription = await _repository.FirstOrDefaultAsync(spec, cancellationToken);

        if (subscription is null)
        {
            return Result.Failure<WebhookDeliveryLogDto>(
                Error.NotFound($"Webhook subscription with ID '{command.Id}' not found.", "NOIR-WEBHOOK-002"));
        }

        // Build a test payload
        var testEventId = Guid.NewGuid();
        var payload = new Services.WebhookPayload
        {
            EventType = "webhook.test",
            EventId = testEventId,
            Timestamp = DateTimeOffset.UtcNow,
            Data = new { Message = "This is a test webhook delivery.", SubscriptionId = subscription.Id }
        };

        var payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        // Create a delivery log entry
        var deliveryLog = WebhookDeliveryLog.Create(
            subscription.Id,
            "webhook.test",
            testEventId,
            subscription.Url,
            payloadJson,
            subscription.CustomHeaders,
            _currentUser.TenantId);

        _dbContext.WebhookDeliveryLogs.Add(deliveryLog);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Publish delivery command
        var deliverCommand = new DeliverWebhook.DeliverWebhookCommand(
            subscription.Id,
            deliveryLog.Id,
            "webhook.test",
            testEventId,
            payloadJson,
            subscription.Secret,
            subscription.Url,
            subscription.TimeoutSeconds,
            subscription.MaxRetries,
            1,
            subscription.CustomHeaders);

        await _messageBus.PublishAsync(deliverCommand);

        _logger.LogInformation(
            "Published test webhook delivery for subscription {SubscriptionId}", subscription.Id);

        return Result.Success(WebhookMapper.ToDto(deliveryLog));
    }
}
