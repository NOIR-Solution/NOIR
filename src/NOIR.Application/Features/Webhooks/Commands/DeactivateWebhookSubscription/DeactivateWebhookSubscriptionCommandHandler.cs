namespace NOIR.Application.Features.Webhooks.Commands.DeactivateWebhookSubscription;

/// <summary>
/// Wolverine handler for deactivating a webhook subscription.
/// </summary>
public class DeactivateWebhookSubscriptionCommandHandler
{
    private readonly IRepository<WebhookSubscription, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateWebhookSubscriptionCommandHandler(
        IRepository<WebhookSubscription, Guid> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<WebhookSubscriptionDto>> Handle(
        DeactivateWebhookSubscriptionCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new Webhooks.Specifications.WebhookSubscriptionByIdForUpdateSpec(command.Id);
        var subscription = await _repository.FirstOrDefaultAsync(spec, cancellationToken);

        if (subscription is null)
        {
            return Result.Failure<WebhookSubscriptionDto>(
                Error.NotFound($"Webhook subscription with ID '{command.Id}' not found.", "NOIR-WEBHOOK-002"));
        }

        subscription.Deactivate();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(WebhookMapper.ToDto(subscription));
    }
}
