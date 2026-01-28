namespace NOIR.Application.Features.Products.Commands.AddProductOption;

/// <summary>
/// Validator for AddProductOptionCommand.
/// </summary>
public sealed class AddProductOptionCommandValidator : AbstractValidator<AddProductOptionCommand>
{
    private const int MaxNameLength = 50;
    private const int MaxDisplayNameLength = 100;

    public AddProductOptionCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Option name is required.")
            .MaximumLength(MaxNameLength).WithMessage($"Option name cannot exceed {MaxNameLength} characters.");

        RuleFor(x => x.DisplayName)
            .MaximumLength(MaxDisplayNameLength).WithMessage($"Display name cannot exceed {MaxDisplayNameLength} characters.")
            .When(x => x.DisplayName is not null);

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Sort order must be non-negative.");

        RuleForEach(x => x.Values).ChildRules(value =>
        {
            value.RuleFor(v => v.Value)
                .NotEmpty().WithMessage("Value is required.")
                .MaximumLength(50).WithMessage("Value cannot exceed 50 characters.");

            value.RuleFor(v => v.DisplayValue)
                .MaximumLength(100).WithMessage("Display value cannot exceed 100 characters.")
                .When(v => v.DisplayValue is not null);

            value.RuleFor(v => v.ColorCode)
                .Matches(@"^#[0-9A-Fa-f]{6}$").WithMessage("Color code must be a valid hex color (e.g., #FF0000).")
                .When(v => v.ColorCode is not null);

            value.RuleFor(v => v.SwatchUrl)
                .MaximumLength(500).WithMessage("Swatch URL cannot exceed 500 characters.")
                .When(v => v.SwatchUrl is not null);
        }).When(x => x.Values is not null);
    }
}
