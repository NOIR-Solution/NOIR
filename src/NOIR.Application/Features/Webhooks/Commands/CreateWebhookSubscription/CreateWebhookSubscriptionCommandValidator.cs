using NOIR.Application.Features.Webhooks.Common;

namespace NOIR.Application.Features.Webhooks.Commands.CreateWebhookSubscription;

/// <summary>
/// Validator for CreateWebhookSubscriptionCommand.
/// </summary>
public sealed class CreateWebhookSubscriptionCommandValidator : AbstractValidator<CreateWebhookSubscriptionCommand>
{
    public CreateWebhookSubscriptionCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Webhook subscription name is required.")
            .MaximumLength(200).WithMessage("Webhook subscription name cannot exceed 200 characters.");

        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("Webhook URL is required.")
            .MaximumLength(2048).WithMessage("Webhook URL cannot exceed 2048 characters.")
            .Must(url => url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Webhook URL must start with 'https://'.")
            .Must(url => !WebhookUrlValidator.IsBlockedUrl(url))
            .WithMessage("Webhook URL must not target private, internal, or loopback addresses.");

        RuleFor(x => x.EventPatterns)
            .NotEmpty().WithMessage("At least one event pattern is required.")
            .MaximumLength(2000).WithMessage("Event patterns cannot exceed 2000 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");

        RuleFor(x => x.CustomHeaders)
            .MaximumLength(4000).WithMessage("Custom headers cannot exceed 4000 characters.");

        RuleFor(x => x.MaxRetries)
            .InclusiveBetween(0, 10).WithMessage("Max retries must be between 0 and 10.");

        RuleFor(x => x.TimeoutSeconds)
            .InclusiveBetween(5, 60).WithMessage("Timeout must be between 5 and 60 seconds.");
    }
}
