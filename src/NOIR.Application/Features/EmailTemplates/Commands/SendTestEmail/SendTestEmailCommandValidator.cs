namespace NOIR.Application.Features.EmailTemplates.Commands.SendTestEmail;

/// <summary>
/// Validator for SendTestEmailCommand.
/// </summary>
public sealed class SendTestEmailCommandValidator : AbstractValidator<SendTestEmailCommand>
{
    public SendTestEmailCommandValidator()
    {
        RuleFor(x => x.TemplateId)
            .NotEmpty().WithMessage("Template ID is required.");

        RuleFor(x => x.RecipientEmail)
            .NotEmpty().WithMessage("Recipient email is required.")
            .EmailAddress().WithMessage("Invalid email address format.");

        RuleFor(x => x.SampleData)
            .NotNull().WithMessage("Sample data is required.");
    }
}
