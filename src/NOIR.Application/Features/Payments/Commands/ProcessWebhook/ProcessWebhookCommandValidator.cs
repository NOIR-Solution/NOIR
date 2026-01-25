namespace NOIR.Application.Features.Payments.Commands.ProcessWebhook;

/// <summary>
/// Validator for ProcessWebhookCommand.
/// </summary>
public sealed class ProcessWebhookCommandValidator : AbstractValidator<ProcessWebhookCommand>
{
    private const int MaxProviderLength = 50;
    private const int MaxPayloadLength = 1048576; // 1MB

    public ProcessWebhookCommandValidator()
    {
        RuleFor(x => x.Provider)
            .NotEmpty().WithMessage("Provider is required.")
            .MaximumLength(MaxProviderLength).WithMessage($"Provider cannot exceed {MaxProviderLength} characters.");

        RuleFor(x => x.RawPayload)
            .NotEmpty().WithMessage("Webhook payload is required.")
            .MaximumLength(MaxPayloadLength).WithMessage("Webhook payload exceeds maximum size.");
    }
}
