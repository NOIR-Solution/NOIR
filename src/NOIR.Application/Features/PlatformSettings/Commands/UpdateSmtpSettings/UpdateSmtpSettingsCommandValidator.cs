namespace NOIR.Application.Features.PlatformSettings.Commands.UpdateSmtpSettings;

/// <summary>
/// Validator for UpdateSmtpSettingsCommand.
/// </summary>
public class UpdateSmtpSettingsCommandValidator : AbstractValidator<UpdateSmtpSettingsCommand>
{
    public UpdateSmtpSettingsCommandValidator()
    {
        RuleFor(x => x.Host)
            .NotEmpty().WithMessage("SMTP host is required")
            .MaximumLength(255).WithMessage("SMTP host cannot exceed 255 characters");

        RuleFor(x => x.Port)
            .InclusiveBetween(1, 65535).WithMessage("Port must be between 1 and 65535");

        RuleFor(x => x.Username)
            .MaximumLength(255).WithMessage("Username cannot exceed 255 characters")
            .When(x => x.Username is not null);

        RuleFor(x => x.Password)
            .MaximumLength(500).WithMessage("Password cannot exceed 500 characters")
            .When(x => x.Password is not null);

        RuleFor(x => x.FromEmail)
            .NotEmpty().WithMessage("From email is required")
            .EmailAddress().WithMessage("From email must be a valid email address")
            .MaximumLength(255).WithMessage("From email cannot exceed 255 characters");

        RuleFor(x => x.FromName)
            .NotEmpty().WithMessage("From name is required")
            .MaximumLength(100).WithMessage("From name cannot exceed 100 characters");
    }
}
