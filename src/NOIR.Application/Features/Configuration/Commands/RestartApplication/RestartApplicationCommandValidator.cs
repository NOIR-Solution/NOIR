namespace NOIR.Application.Features.Configuration.Commands.RestartApplication;

/// <summary>
/// Validator for RestartApplicationCommand.
/// Ensures a meaningful reason is provided for audit trail.
/// </summary>
public class RestartApplicationCommandValidator : AbstractValidator<RestartApplicationCommand>
{
    public RestartApplicationCommandValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Restart reason is required for audit trail.")
            .MinimumLength(5)
            .WithMessage("Restart reason must be at least 5 characters.")
            .MaximumLength(500)
            .WithMessage("Restart reason must not exceed 500 characters.");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required for audit trail.");
    }
}
