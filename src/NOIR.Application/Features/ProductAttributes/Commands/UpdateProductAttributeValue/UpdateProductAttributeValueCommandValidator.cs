namespace NOIR.Application.Features.ProductAttributes.Commands.UpdateProductAttributeValue;

/// <summary>
/// Validator for UpdateProductAttributeValueCommand.
/// </summary>
public class UpdateProductAttributeValueCommandValidator : AbstractValidator<UpdateProductAttributeValueCommand>
{
    public UpdateProductAttributeValueCommandValidator()
    {
        RuleFor(x => x.AttributeId)
            .NotEmpty().WithMessage("Attribute ID is required.");

        RuleFor(x => x.ValueId)
            .NotEmpty().WithMessage("Value ID is required.");

        RuleFor(x => x.Value)
            .NotEmpty().WithMessage("Value is required.")
            .MaximumLength(200).WithMessage("Value must not exceed 200 characters.");

        RuleFor(x => x.DisplayValue)
            .NotEmpty().WithMessage("Display value is required.")
            .MaximumLength(200).WithMessage("Display value must not exceed 200 characters.");

        RuleFor(x => x.ColorCode)
            .MaximumLength(20).WithMessage("Color code must not exceed 20 characters.")
            .Matches("^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$")
            .When(x => !string.IsNullOrEmpty(x.ColorCode))
            .WithMessage("Color code must be a valid hex color (e.g., #FF0000 or #F00).");

        RuleFor(x => x.SwatchUrl)
            .MaximumLength(500).WithMessage("Swatch URL must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.SwatchUrl));

        RuleFor(x => x.IconUrl)
            .MaximumLength(500).WithMessage("Icon URL must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.IconUrl));

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Sort order must be non-negative.");
    }
}
