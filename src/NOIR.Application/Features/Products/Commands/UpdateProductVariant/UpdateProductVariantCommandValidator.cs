namespace NOIR.Application.Features.Products.Commands.UpdateProductVariant;

/// <summary>
/// Validator for UpdateProductVariantCommand.
/// </summary>
public sealed class UpdateProductVariantCommandValidator : AbstractValidator<UpdateProductVariantCommand>
{
    private const int MaxNameLength = 100;
    private const int MaxSkuLength = 50;

    public UpdateProductVariantCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required.");

        RuleFor(x => x.VariantId)
            .NotEmpty().WithMessage("Variant ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Variant name is required.")
            .MaximumLength(MaxNameLength).WithMessage($"Variant name cannot exceed {MaxNameLength} characters.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Price must be non-negative.");

        RuleFor(x => x.Sku)
            .MaximumLength(MaxSkuLength).WithMessage($"SKU cannot exceed {MaxSkuLength} characters.")
            .When(x => x.Sku is not null);

        RuleFor(x => x.CompareAtPrice)
            .GreaterThan(0).WithMessage("Compare-at price must be positive.")
            .GreaterThan(x => x.Price).WithMessage("Compare-at price must be higher than the regular price.")
            .When(x => x.CompareAtPrice.HasValue);

        RuleFor(x => x.CostPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Cost price must be non-negative.")
            .When(x => x.CostPrice.HasValue);

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Stock quantity must be non-negative.");

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Sort order must be non-negative.");
    }
}
