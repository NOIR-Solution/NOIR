namespace NOIR.Application.Features.EmailTemplates.Commands.UpdateEmailTemplate;

/// <summary>
/// Validator for UpdateEmailTemplateCommand.
/// </summary>
public sealed class UpdateEmailTemplateCommandValidator : AbstractValidator<UpdateEmailTemplateCommand>
{
    private const int MaxSubjectLength = 500;
    private const int MaxDescriptionLength = 1000;

    public UpdateEmailTemplateCommandValidator(ILocalizationService localization)
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Template ID is required.");

        RuleFor(x => x.Subject)
            .NotEmpty().WithMessage("Subject is required.")
            .MaximumLength(MaxSubjectLength).WithMessage($"Subject cannot exceed {MaxSubjectLength} characters.");

        RuleFor(x => x.HtmlBody)
            .NotEmpty().WithMessage("HTML body is required.");

        RuleFor(x => x.Description)
            .MaximumLength(MaxDescriptionLength).WithMessage($"Description cannot exceed {MaxDescriptionLength} characters.")
            .When(x => x.Description is not null);
    }
}
