namespace NOIR.Application.Features.Webhooks.Commands.TestWebhookSubscription;

/// <summary>
/// Validator for TestWebhookSubscriptionCommand.
/// </summary>
public sealed class TestWebhookSubscriptionCommandValidator : AbstractValidator<TestWebhookSubscriptionCommand>
{
    public TestWebhookSubscriptionCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Webhook subscription ID is required.");
    }
}
