namespace NOIR.Application.Features.Webhooks.Commands.CreateWebhookSubscription;

/// <summary>
/// Wolverine handler for creating a new webhook subscription.
/// </summary>
public class CreateWebhookSubscriptionCommandHandler
{
    private readonly IRepository<WebhookSubscription, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public CreateWebhookSubscriptionCommandHandler(
        IRepository<WebhookSubscription, Guid> repository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IEntityUpdateHubContext entityUpdateHub)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _entityUpdateHub = entityUpdateHub;
    }

    public async Task<Result<WebhookSubscriptionDto>> Handle(
        CreateWebhookSubscriptionCommand command,
        CancellationToken cancellationToken)
    {
        // Check for duplicate URL within the tenant
        var urlSpec = new Webhooks.Specifications.WebhookSubscriptionByUrlSpec(command.Url);
        var existing = await _repository.FirstOrDefaultAsync(urlSpec, cancellationToken);
        if (existing is not null)
        {
            return Result.Failure<WebhookSubscriptionDto>(
                Error.Validation("Url", $"A webhook subscription for URL '{command.Url}' already exists.", "NOIR-WEBHOOK-001"));
        }

        var subscription = WebhookSubscription.Create(
            command.Name,
            command.Url,
            command.EventPatterns,
            command.Description,
            command.CustomHeaders,
            command.MaxRetries,
            command.TimeoutSeconds,
            _currentUser.TenantId);

        await _repository.AddAsync(subscription, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "WebhookSubscription",
            entityId: subscription.Id,
            operation: EntityOperation.Created,
            tenantId: _currentUser.TenantId!,
            ct: cancellationToken);

        return Result.Success(WebhookMapper.ToDto(subscription));
    }
}
