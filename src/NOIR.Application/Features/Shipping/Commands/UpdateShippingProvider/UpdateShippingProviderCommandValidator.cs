namespace NOIR.Application.Features.Shipping.Commands.UpdateShippingProvider;

/// <summary>
/// Validator for UpdateShippingProviderCommand.
/// </summary>
public class UpdateShippingProviderCommandValidator : AbstractValidator<UpdateShippingProviderCommand>
{
    public UpdateShippingProviderCommandValidator()
    {
        RuleFor(x => x.ProviderId)
            .NotEmpty()
            .WithMessage("Provider ID is required.");

        RuleFor(x => x.DisplayName)
            .MaximumLength(200)
            .WithMessage("Display name must not exceed 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.DisplayName));

        RuleFor(x => x.Environment)
            .IsInEnum()
            .WithMessage("Invalid environment.")
            .When(x => x.Environment.HasValue);

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Sort order must be non-negative.")
            .When(x => x.SortOrder.HasValue);

        RuleFor(x => x.ApiBaseUrl)
            .Must(url => string.IsNullOrEmpty(url) || Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("API base URL must be a valid URL.")
            .When(x => !string.IsNullOrEmpty(x.ApiBaseUrl));

        RuleFor(x => x.TrackingUrlTemplate)
            .MaximumLength(500)
            .WithMessage("Tracking URL template must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.TrackingUrlTemplate));

        RuleFor(x => x.MinWeightGrams)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Minimum weight must be non-negative.")
            .When(x => x.MinWeightGrams.HasValue);

        RuleFor(x => x.MaxWeightGrams)
            .GreaterThan(0)
            .WithMessage("Maximum weight must be positive.")
            .When(x => x.MaxWeightGrams.HasValue);

        RuleFor(x => x.MinCodAmount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Minimum COD amount must be non-negative.")
            .When(x => x.MinCodAmount.HasValue);

        RuleFor(x => x.MaxCodAmount)
            .GreaterThan(0)
            .WithMessage("Maximum COD amount must be positive.")
            .When(x => x.MaxCodAmount.HasValue);
    }
}
