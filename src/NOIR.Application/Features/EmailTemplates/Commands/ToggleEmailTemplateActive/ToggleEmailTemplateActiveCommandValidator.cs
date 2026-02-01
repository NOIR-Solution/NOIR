namespace NOIR.Application.Features.EmailTemplates.Commands.ToggleEmailTemplateActive;

/// <summary>
/// Validator for ToggleEmailTemplateActiveCommand.
/// </summary>
public sealed class ToggleEmailTemplateActiveCommandValidator : AbstractValidator<ToggleEmailTemplateActiveCommand>
{
    public ToggleEmailTemplateActiveCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Email template ID is required.");
    }
}
