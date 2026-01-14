namespace NOIR.Application.Features.Auth.Commands.PasswordReset.ResendPasswordResetOtp;

/// <summary>
/// Validator for ResendPasswordResetOtpCommand.
/// </summary>
public class ResendPasswordResetOtpCommandValidator : AbstractValidator<ResendPasswordResetOtpCommand>
{
    public ResendPasswordResetOtpCommandValidator(ILocalizationService localization)
    {
        RuleFor(x => x.SessionToken)
            .NotEmpty()
            .WithMessage(localization["validation.required"]);
    }
}
