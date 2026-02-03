namespace NOIR.Application.Features.Payments.Commands.ConfigureGateway;

/// <summary>
/// Validator for ConfigureGatewayCommand.
/// </summary>
public sealed class ConfigureGatewayCommandValidator : AbstractValidator<ConfigureGatewayCommand>
{
    private const int MaxProviderLength = 50;
    private const int MaxDisplayNameLength = 200;

    public ConfigureGatewayCommandValidator()
    {
        RuleFor(x => x.Provider)
            .NotEmpty().WithMessage("Provider is required.")
            .MaximumLength(MaxProviderLength).WithMessage($"Provider cannot exceed {MaxProviderLength} characters.");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name is required.")
            .MaximumLength(MaxDisplayNameLength).WithMessage($"Display name cannot exceed {MaxDisplayNameLength} characters.");

        RuleFor(x => x.Environment)
            .IsInEnum().WithMessage("Invalid gateway environment.");

        RuleFor(x => x.Credentials)
            .NotNull().WithMessage("Credentials are required.")
            .Must(c => c is not null && c.Count > 0).WithMessage("At least one credential is required.")
            .When(x => x.Credentials is not null);

        RuleFor(x => x.SupportedMethods)
            .NotNull().WithMessage("Supported methods are required.")
            .Must(m => m is not null && m.Count > 0).WithMessage("At least one supported payment method is required.")
            .When(x => x.SupportedMethods is not null);

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Sort order must be non-negative.");
    }
}
