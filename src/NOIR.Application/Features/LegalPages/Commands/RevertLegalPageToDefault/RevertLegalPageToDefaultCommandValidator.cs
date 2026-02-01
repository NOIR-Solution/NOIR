namespace NOIR.Application.Features.LegalPages.Commands.RevertLegalPageToDefault;

/// <summary>
/// Validator for RevertLegalPageToDefaultCommand.
/// </summary>
public sealed class RevertLegalPageToDefaultCommandValidator : AbstractValidator<RevertLegalPageToDefaultCommand>
{
    public RevertLegalPageToDefaultCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Legal page ID is required.");
    }
}
