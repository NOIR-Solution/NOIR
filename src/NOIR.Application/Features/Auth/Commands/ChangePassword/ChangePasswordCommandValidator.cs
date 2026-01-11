namespace NOIR.Application.Features.Auth.Commands.ChangePassword;

/// <summary>
/// Validator for ChangePasswordCommand.
/// Uses same password policy as registration (6 chars minimum).
/// Additional complexity requirements enforced by ASP.NET Identity based on appsettings.
/// </summary>
public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    private const int MinPasswordLength = 6;

    public ChangePasswordCommandValidator(ILocalizationService localization)
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage(localization["validation.currentPassword.required"]);

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage(localization["validation.newPassword.required"])
            .MinimumLength(MinPasswordLength).WithMessage(localization.Get("validation.password.tooShort", MinPasswordLength));

        RuleFor(x => x.NewPassword)
            .NotEqual(x => x.CurrentPassword)
            .WithMessage(localization["validation.password.mustBeDifferent"]);
    }
}
