namespace NOIR.Application.Features.Auth.Commands.PasswordReset.VerifyPasswordResetOtp;

/// <summary>
/// Validator for VerifyPasswordResetOtpCommand.
/// </summary>
public class VerifyPasswordResetOtpCommandValidator : AbstractValidator<VerifyPasswordResetOtpCommand>
{
    public VerifyPasswordResetOtpCommandValidator(ILocalizationService localization)
    {
        RuleFor(x => x.SessionToken)
            .NotEmpty()
            .WithMessage(localization["validation.required"]);

        RuleFor(x => x.Otp)
            .NotEmpty()
            .WithMessage(localization["validation.required"])
            .Length(6)
            .WithMessage(localization["validation.exactLength"]);
    }
}
