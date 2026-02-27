namespace NOIR.Application.Features.Webhooks.Commands.RotateWebhookSecret;

/// <summary>
/// Wolverine handler for rotating a webhook subscription's HMAC-SHA256 secret.
/// </summary>
public class RotateWebhookSecretCommandHandler
{
    private readonly IRepository<WebhookSubscription, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public RotateWebhookSecretCommandHandler(
        IRepository<WebhookSubscription, Guid> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<WebhookSecretDto>> Handle(
        RotateWebhookSecretCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new Webhooks.Specifications.WebhookSubscriptionByIdForUpdateSpec(command.Id);
        var subscription = await _repository.FirstOrDefaultAsync(spec, cancellationToken);

        if (subscription is null)
        {
            return Result.Failure<WebhookSecretDto>(
                Error.NotFound($"Webhook subscription with ID '{command.Id}' not found.", "NOIR-WEBHOOK-002"));
        }

        var newSecret = subscription.RotateSecret();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new WebhookSecretDto { Secret = newSecret });
    }
}
