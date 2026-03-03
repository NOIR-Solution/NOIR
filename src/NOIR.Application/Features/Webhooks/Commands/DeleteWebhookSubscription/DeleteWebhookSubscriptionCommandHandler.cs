namespace NOIR.Application.Features.Webhooks.Commands.DeleteWebhookSubscription;

/// <summary>
/// Wolverine handler for soft deleting a webhook subscription.
/// </summary>
public class DeleteWebhookSubscriptionCommandHandler
{
    private readonly IRepository<WebhookSubscription, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public DeleteWebhookSubscriptionCommandHandler(
        IRepository<WebhookSubscription, Guid> repository,
        IUnitOfWork unitOfWork,
        IEntityUpdateHubContext entityUpdateHub)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _entityUpdateHub = entityUpdateHub;
    }

    public async Task<Result<bool>> Handle(
        DeleteWebhookSubscriptionCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new Webhooks.Specifications.WebhookSubscriptionByIdForUpdateSpec(command.Id);
        var subscription = await _repository.FirstOrDefaultAsync(spec, cancellationToken);

        if (subscription is null)
        {
            return Result.Failure<bool>(
                Error.NotFound($"Webhook subscription with ID '{command.Id}' not found.", "NOIR-WEBHOOK-002"));
        }

        _repository.Remove(subscription);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "WebhookSubscription",
            entityId: subscription.Id,
            operation: EntityOperation.Deleted,
            tenantId: subscription.TenantId!,
            ct: cancellationToken);

        return Result.Success(true);
    }
}
