namespace NOIR.Application.Features.TenantSettings.Commands.UpdateTenantSmtpSettings;

/// <summary>
/// Validator for UpdateTenantSmtpSettingsCommand.
/// </summary>
public class UpdateTenantSmtpSettingsCommandValidator : AbstractValidator<UpdateTenantSmtpSettingsCommand>
{
    public UpdateTenantSmtpSettingsCommandValidator()
    {
        RuleFor(x => x.Host)
            .NotEmpty().WithMessage("SMTP host is required.")
            .MaximumLength(255).WithMessage("SMTP host must not exceed 255 characters.");

        RuleFor(x => x.Port)
            .InclusiveBetween(1, 65535).WithMessage("Port must be between 1 and 65535.");

        RuleFor(x => x.FromEmail)
            .NotEmpty().WithMessage("From email is required.")
            .EmailAddress().WithMessage("From email must be a valid email address.")
            .MaximumLength(255).WithMessage("From email must not exceed 255 characters.");

        RuleFor(x => x.FromName)
            .NotEmpty().WithMessage("From name is required.")
            .MaximumLength(100).WithMessage("From name must not exceed 100 characters.");

        RuleFor(x => x.Username)
            .MaximumLength(255).WithMessage("Username must not exceed 255 characters.")
            .When(x => x.Username is not null);
    }
}
