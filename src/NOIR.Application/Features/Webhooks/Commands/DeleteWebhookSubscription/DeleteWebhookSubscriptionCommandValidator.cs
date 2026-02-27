namespace NOIR.Application.Features.Webhooks.Commands.DeleteWebhookSubscription;

/// <summary>
/// Validator for DeleteWebhookSubscriptionCommand.
/// </summary>
public sealed class DeleteWebhookSubscriptionCommandValidator : AbstractValidator<DeleteWebhookSubscriptionCommand>
{
    public DeleteWebhookSubscriptionCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Webhook subscription ID is required.");
    }
}
