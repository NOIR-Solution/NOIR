namespace NOIR.Application.Features.Auth.Commands.PasswordReset.ResetPassword;

/// <summary>
/// Validator for ResetPasswordCommand.
/// </summary>
public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator(ILocalizationService localization)
    {
        RuleFor(x => x.ResetToken)
            .NotEmpty()
            .WithMessage(localization["validation.required"]);

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage(localization["validation.required"])
            .MinimumLength(6)
            .WithMessage(localization["validation.minLength"]);
    }
}
