namespace NOIR.Application.Features.Webhooks.Commands.ActivateWebhookSubscription;

/// <summary>
/// Wolverine handler for activating a webhook subscription.
/// </summary>
public class ActivateWebhookSubscriptionCommandHandler
{
    private readonly IRepository<WebhookSubscription, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ActivateWebhookSubscriptionCommandHandler(
        IRepository<WebhookSubscription, Guid> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<WebhookSubscriptionDto>> Handle(
        ActivateWebhookSubscriptionCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new Webhooks.Specifications.WebhookSubscriptionByIdForUpdateSpec(command.Id);
        var subscription = await _repository.FirstOrDefaultAsync(spec, cancellationToken);

        if (subscription is null)
        {
            return Result.Failure<WebhookSubscriptionDto>(
                Error.NotFound($"Webhook subscription with ID '{command.Id}' not found.", "NOIR-WEBHOOK-002"));
        }

        subscription.Activate();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(WebhookMapper.ToDto(subscription));
    }
}
