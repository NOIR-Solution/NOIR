namespace NOIR.Application.Features.Webhooks.Commands.UpdateWebhookSubscription;

/// <summary>
/// Wolverine handler for updating a webhook subscription.
/// </summary>
public class UpdateWebhookSubscriptionCommandHandler
{
    private readonly IRepository<WebhookSubscription, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateWebhookSubscriptionCommandHandler(
        IRepository<WebhookSubscription, Guid> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<WebhookSubscriptionDto>> Handle(
        UpdateWebhookSubscriptionCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new Webhooks.Specifications.WebhookSubscriptionByIdForUpdateSpec(command.Id);
        var subscription = await _repository.FirstOrDefaultAsync(spec, cancellationToken);

        if (subscription is null)
        {
            return Result.Failure<WebhookSubscriptionDto>(
                Error.NotFound($"Webhook subscription with ID '{command.Id}' not found.", "NOIR-WEBHOOK-002"));
        }

        // Check for duplicate URL (different subscription, same URL)
        if (!string.Equals(subscription.Url, command.Url, StringComparison.OrdinalIgnoreCase))
        {
            var urlSpec = new Webhooks.Specifications.WebhookSubscriptionByUrlSpec(command.Url, command.Id);
            var existingWithUrl = await _repository.FirstOrDefaultAsync(urlSpec, cancellationToken);
            if (existingWithUrl is not null)
            {
                return Result.Failure<WebhookSubscriptionDto>(
                    Error.Validation("Url", $"A webhook subscription for URL '{command.Url}' already exists.", "NOIR-WEBHOOK-001"));
            }
        }

        subscription.Update(
            command.Name,
            command.Url,
            command.EventPatterns,
            command.Description,
            command.CustomHeaders,
            command.MaxRetries,
            command.TimeoutSeconds);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(WebhookMapper.ToDto(subscription));
    }
}
