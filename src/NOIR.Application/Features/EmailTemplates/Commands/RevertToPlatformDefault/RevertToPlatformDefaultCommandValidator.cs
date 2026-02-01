namespace NOIR.Application.Features.EmailTemplates.Commands.RevertToPlatformDefault;

/// <summary>
/// Validator for RevertToPlatformDefaultCommand.
/// </summary>
public sealed class RevertToPlatformDefaultCommandValidator : AbstractValidator<RevertToPlatformDefaultCommand>
{
    public RevertToPlatformDefaultCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Email template ID is required.");
    }
}
