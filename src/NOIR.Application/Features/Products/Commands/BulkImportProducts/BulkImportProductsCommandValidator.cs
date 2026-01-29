namespace NOIR.Application.Features.Products.Commands.BulkImportProducts;

public class BulkImportProductsCommandValidator : AbstractValidator<BulkImportProductsCommand>
{
    private const int MaxImageCount = 10;
    private const int MaxUrlLength = 2000;

    public BulkImportProductsCommandValidator()
    {
        RuleFor(x => x.Products)
            .NotEmpty()
            .WithMessage("At least one product is required.");

        RuleFor(x => x.Products.Count)
            .LessThanOrEqualTo(ProductConstants.MaxBulkOperationSize)
            .WithMessage($"Maximum {ProductConstants.MaxBulkOperationSize} products per import.");

        RuleForEach(x => x.Products).ChildRules(product =>
        {
            // Required fields
            product.RuleFor(p => p.Name)
                .NotEmpty()
                .WithMessage("Product name is required.")
                .MaximumLength(200)
                .WithMessage("Product name must not exceed 200 characters.");

            product.RuleFor(p => p.BasePrice)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Base price must be 0 or greater.");

            // Optional text fields
            product.RuleFor(p => p.Slug)
                .MaximumLength(200)
                .When(p => !string.IsNullOrEmpty(p.Slug))
                .WithMessage("Slug must not exceed 200 characters.");

            product.RuleFor(p => p.Currency)
                .MaximumLength(3)
                .When(p => !string.IsNullOrEmpty(p.Currency))
                .WithMessage("Currency code must not exceed 3 characters.");

            product.RuleFor(p => p.ShortDescription)
                .MaximumLength(500)
                .When(p => !string.IsNullOrEmpty(p.ShortDescription))
                .WithMessage("Short description must not exceed 500 characters.");

            product.RuleFor(p => p.Sku)
                .MaximumLength(100)
                .When(p => !string.IsNullOrEmpty(p.Sku))
                .WithMessage("SKU must not exceed 100 characters.");

            product.RuleFor(p => p.Barcode)
                .MaximumLength(100)
                .When(p => !string.IsNullOrEmpty(p.Barcode))
                .WithMessage("Barcode must not exceed 100 characters.");

            product.RuleFor(p => p.CategoryName)
                .MaximumLength(100)
                .When(p => !string.IsNullOrEmpty(p.CategoryName))
                .WithMessage("Category name must not exceed 100 characters.");

            product.RuleFor(p => p.Brand)
                .MaximumLength(100)
                .When(p => !string.IsNullOrEmpty(p.Brand))
                .WithMessage("Brand must not exceed 100 characters.");

            // Numeric fields
            product.RuleFor(p => p.Stock)
                .GreaterThanOrEqualTo(0)
                .When(p => p.Stock.HasValue)
                .WithMessage("Stock must be 0 or greater.");

            // Variant fields
            product.RuleFor(p => p.VariantName)
                .MaximumLength(100)
                .When(p => !string.IsNullOrEmpty(p.VariantName))
                .WithMessage("Variant name must not exceed 100 characters.");

            product.RuleFor(p => p.VariantPrice)
                .GreaterThanOrEqualTo(0)
                .When(p => p.VariantPrice.HasValue)
                .WithMessage("Variant price must be 0 or greater.");

            product.RuleFor(p => p.CompareAtPrice)
                .GreaterThanOrEqualTo(0)
                .When(p => p.CompareAtPrice.HasValue)
                .WithMessage("Compare at price must be 0 or greater.");

            // Images validation (pipe-separated URLs)
            product.RuleFor(p => p.Images)
                .Must(images => ValidateImages(images))
                .When(p => !string.IsNullOrEmpty(p.Images))
                .WithMessage($"Images must be valid URLs (max {MaxImageCount} images, max {MaxUrlLength} chars per URL).");
        });
    }

    private static bool ValidateImages(string? images)
    {
        if (string.IsNullOrEmpty(images)) return true;

        var urls = images.Split('|', StringSplitOptions.RemoveEmptyEntries);

        // Check max count
        if (urls.Length > MaxImageCount) return false;

        // Check each URL length and basic format
        foreach (var url in urls)
        {
            var trimmed = url.Trim();
            if (trimmed.Length > MaxUrlLength) return false;
            if (!trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }
}
