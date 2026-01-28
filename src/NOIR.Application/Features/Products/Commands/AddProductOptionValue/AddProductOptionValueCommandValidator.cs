namespace NOIR.Application.Features.Products.Commands.AddProductOptionValue;

/// <summary>
/// Validator for AddProductOptionValueCommand.
/// </summary>
public sealed class AddProductOptionValueCommandValidator : AbstractValidator<AddProductOptionValueCommand>
{
    private const int MaxValueLength = 50;
    private const int MaxDisplayValueLength = 100;
    private const int MaxSwatchUrlLength = 500;

    public AddProductOptionValueCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required.");

        RuleFor(x => x.OptionId)
            .NotEmpty().WithMessage("Option ID is required.");

        RuleFor(x => x.Value)
            .NotEmpty().WithMessage("Value is required.")
            .MaximumLength(MaxValueLength).WithMessage($"Value cannot exceed {MaxValueLength} characters.");

        RuleFor(x => x.DisplayValue)
            .MaximumLength(MaxDisplayValueLength).WithMessage($"Display value cannot exceed {MaxDisplayValueLength} characters.")
            .When(x => x.DisplayValue is not null);

        RuleFor(x => x.ColorCode)
            .Matches(@"^#[0-9A-Fa-f]{6}$").WithMessage("Color code must be a valid hex color (e.g., #FF0000).")
            .When(x => x.ColorCode is not null);

        RuleFor(x => x.SwatchUrl)
            .MaximumLength(MaxSwatchUrlLength).WithMessage($"Swatch URL cannot exceed {MaxSwatchUrlLength} characters.")
            .When(x => x.SwatchUrl is not null);

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Sort order must be non-negative.");
    }
}
