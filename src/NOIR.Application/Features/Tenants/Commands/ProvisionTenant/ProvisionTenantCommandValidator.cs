namespace NOIR.Application.Features.Tenants.Commands.ProvisionTenant;

/// <summary>
/// Validator for ProvisionTenantCommand.
/// </summary>
public class ProvisionTenantCommandValidator : AbstractValidator<ProvisionTenantCommand>
{
    public ProvisionTenantCommandValidator(ILocalizationService localization)
    {
        // Identifier validation
        RuleFor(x => x.Identifier)
            .NotEmpty()
            .WithMessage(localization["validation.required"])
            .MinimumLength(2)
            .WithMessage(string.Format(localization["validation.minLength"], 2))
            .MaximumLength(64)
            .WithMessage(string.Format(localization["validation.maxLength"], 64))
            .Matches("^[a-z0-9-]+$")
            .WithMessage(localization["validation.tenants.identifierFormat"] ??
                         "Identifier must contain only lowercase letters, numbers, and hyphens");

        // Name validation
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage(localization["validation.required"])
            .MinimumLength(2)
            .WithMessage(string.Format(localization["validation.minLength"], 2))
            .MaximumLength(256)
            .WithMessage(string.Format(localization["validation.maxLength"], 256));

        // Domain validation (optional)
        RuleFor(x => x.Domain)
            .MaximumLength(256)
            .WithMessage(string.Format(localization["validation.maxLength"], 256))
            .Matches(@"^[a-z0-9][a-z0-9.-]*[a-z0-9]$")
            .WithMessage(localization["validation.tenants.domainFormat"] ??
                         "Domain must be a valid hostname format")
            .When(x => !string.IsNullOrWhiteSpace(x.Domain));

        // Description validation (optional)
        RuleFor(x => x.Description)
            .MaximumLength(1024)
            .WithMessage(string.Format(localization["validation.maxLength"], 1024))
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        // Note validation (optional)
        RuleFor(x => x.Note)
            .MaximumLength(4096)
            .WithMessage(string.Format(localization["validation.maxLength"], 4096))
            .When(x => !string.IsNullOrWhiteSpace(x.Note));

        // Admin user validation (conditional)
        When(x => x.CreateAdminUser, () =>
        {
            RuleFor(x => x.AdminEmail)
                .NotEmpty()
                .WithMessage(localization["validation.required"] ?? "Admin email is required when CreateAdminUser is true")
                .EmailAddress()
                .WithMessage(localization["validation.email"] ?? "Invalid email address format");

            RuleFor(x => x.AdminPassword)
                .NotEmpty()
                .WithMessage(localization["validation.required"] ?? "Admin password is required when CreateAdminUser is true")
                .MinimumLength(6)
                .WithMessage(string.Format(localization["validation.minLength"], 6));

            RuleFor(x => x.AdminFirstName)
                .MaximumLength(64)
                .WithMessage(string.Format(localization["validation.maxLength"], 64))
                .When(x => !string.IsNullOrWhiteSpace(x.AdminFirstName));

            RuleFor(x => x.AdminLastName)
                .MaximumLength(64)
                .WithMessage(string.Format(localization["validation.maxLength"], 64))
                .When(x => !string.IsNullOrWhiteSpace(x.AdminLastName));
        });
    }
}
