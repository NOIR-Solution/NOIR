namespace NOIR.Application.Features.Shipping.Commands.ConfigureShippingProvider;

/// <summary>
/// Validator for ConfigureShippingProviderCommand.
/// </summary>
public class ConfigureShippingProviderCommandValidator : AbstractValidator<ConfigureShippingProviderCommand>
{
    public ConfigureShippingProviderCommandValidator()
    {
        RuleFor(x => x.ProviderCode)
            .IsInEnum()
            .WithMessage("Invalid shipping provider code.");

        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .WithMessage("Display name is required.")
            .MaximumLength(200)
            .WithMessage("Display name must not exceed 200 characters.");

        RuleFor(x => x.Environment)
            .IsInEnum()
            .WithMessage("Invalid environment.");

        RuleFor(x => x.Credentials)
            .NotNull()
            .WithMessage("Credentials are required.");

        RuleFor(x => x.SupportedServices)
            .NotEmpty()
            .WithMessage("At least one supported service is required.");

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Sort order must be non-negative.");

        RuleFor(x => x.ApiBaseUrl)
            .Must(url => string.IsNullOrEmpty(url) || Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("API base URL must be a valid URL.");

        RuleFor(x => x.TrackingUrlTemplate)
            .MaximumLength(500)
            .WithMessage("Tracking URL template must not exceed 500 characters.");
    }
}
