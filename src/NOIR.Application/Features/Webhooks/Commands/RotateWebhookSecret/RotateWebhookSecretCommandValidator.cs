namespace NOIR.Application.Features.Webhooks.Commands.RotateWebhookSecret;

/// <summary>
/// Validator for RotateWebhookSecretCommand.
/// </summary>
public sealed class RotateWebhookSecretCommandValidator : AbstractValidator<RotateWebhookSecretCommand>
{
    public RotateWebhookSecretCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Webhook subscription ID is required.");
    }
}
