namespace NOIR.Application.Features.Webhooks.Commands.DeactivateWebhookSubscription;

/// <summary>
/// Validator for DeactivateWebhookSubscriptionCommand.
/// </summary>
public sealed class DeactivateWebhookSubscriptionCommandValidator : AbstractValidator<DeactivateWebhookSubscriptionCommand>
{
    public DeactivateWebhookSubscriptionCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Webhook subscription ID is required.");
    }
}
