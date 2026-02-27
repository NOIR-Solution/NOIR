namespace NOIR.Application.Features.Webhooks.Commands.ActivateWebhookSubscription;

/// <summary>
/// Validator for ActivateWebhookSubscriptionCommand.
/// </summary>
public sealed class ActivateWebhookSubscriptionCommandValidator : AbstractValidator<ActivateWebhookSubscriptionCommand>
{
    public ActivateWebhookSubscriptionCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Webhook subscription ID is required.");
    }
}
