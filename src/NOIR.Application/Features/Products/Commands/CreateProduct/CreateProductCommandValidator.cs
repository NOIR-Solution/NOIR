namespace NOIR.Application.Features.Products.Commands.CreateProduct;

/// <summary>
/// Validator for CreateProductCommand.
/// </summary>
public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    private const int MaxNameLength = 500;
    private const int MaxSlugLength = 500;
    private const int MaxShortDescriptionLength = 300;
    private const int MaxDescriptionLength = 10000;
    private const int MaxBrandLength = 200;
    private const int MaxSkuLength = 100;
    private const int MaxBarcodeLength = 100;
    private const int MaxMetaTitleLength = 200;
    private const int MaxMetaDescriptionLength = 500;
    private const int MaxUrlLength = 2000;

    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(MaxNameLength).WithMessage($"Name cannot exceed {MaxNameLength} characters.");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required.")
            .MaximumLength(MaxSlugLength).WithMessage($"Slug cannot exceed {MaxSlugLength} characters.")
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$").WithMessage("Slug must be lowercase and contain only letters, numbers, and hyphens.");

        RuleFor(x => x.ShortDescription)
            .MaximumLength(MaxShortDescriptionLength).WithMessage($"Short description cannot exceed {MaxShortDescriptionLength} characters.")
            .When(x => x.ShortDescription is not null);

        RuleFor(x => x.Description)
            .MaximumLength(MaxDescriptionLength).WithMessage($"Description cannot exceed {MaxDescriptionLength} characters.")
            .When(x => x.Description is not null);

        RuleFor(x => x.BasePrice)
            .GreaterThanOrEqualTo(0).WithMessage("Base price must be a non-negative number.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Length(3).WithMessage("Currency code must be 3 characters (ISO 4217).");

        RuleFor(x => x.Brand)
            .MaximumLength(MaxBrandLength).WithMessage($"Brand cannot exceed {MaxBrandLength} characters.")
            .When(x => x.Brand is not null);

        RuleFor(x => x.Sku)
            .MaximumLength(MaxSkuLength).WithMessage($"SKU cannot exceed {MaxSkuLength} characters.")
            .When(x => x.Sku is not null);

        RuleFor(x => x.Barcode)
            .MaximumLength(MaxBarcodeLength).WithMessage($"Barcode cannot exceed {MaxBarcodeLength} characters.")
            .When(x => x.Barcode is not null);

        RuleFor(x => x.Weight)
            .GreaterThanOrEqualTo(0).WithMessage("Weight must be a non-negative number.")
            .When(x => x.Weight.HasValue);

        RuleFor(x => x.MetaTitle)
            .MaximumLength(MaxMetaTitleLength).WithMessage($"Meta title cannot exceed {MaxMetaTitleLength} characters.")
            .When(x => x.MetaTitle is not null);

        RuleFor(x => x.MetaDescription)
            .MaximumLength(MaxMetaDescriptionLength).WithMessage($"Meta description cannot exceed {MaxMetaDescriptionLength} characters.")
            .When(x => x.MetaDescription is not null);

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Sort order must be a non-negative number.");

        // Variant validation
        RuleForEach(x => x.Variants)
            .SetValidator(new CreateProductVariantDtoValidator())
            .When(x => x.Variants is not null);

        // Image validation
        RuleForEach(x => x.Images)
            .SetValidator(new CreateProductImageDtoValidator())
            .When(x => x.Images is not null);
    }
}

/// <summary>
/// Validator for CreateProductVariantDto.
/// </summary>
public sealed class CreateProductVariantDtoValidator : AbstractValidator<CreateProductVariantDto>
{
    private const int MaxNameLength = 200;
    private const int MaxSkuLength = 100;

    public CreateProductVariantDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Variant name is required.")
            .MaximumLength(MaxNameLength).WithMessage($"Variant name cannot exceed {MaxNameLength} characters.");

        RuleFor(x => x.Sku)
            .MaximumLength(MaxSkuLength).WithMessage($"Variant SKU cannot exceed {MaxSkuLength} characters.")
            .When(x => x.Sku is not null);

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Variant price must be a non-negative number.");

        RuleFor(x => x.CompareAtPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Compare at price must be a non-negative number.")
            .When(x => x.CompareAtPrice.HasValue);

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Stock quantity must be a non-negative number.");

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Variant sort order must be a non-negative number.");
    }
}

/// <summary>
/// Validator for CreateProductImageDto.
/// </summary>
public sealed class CreateProductImageDtoValidator : AbstractValidator<CreateProductImageDto>
{
    private const int MaxUrlLength = 2000;
    private const int MaxAltTextLength = 500;

    public CreateProductImageDtoValidator()
    {
        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("Image URL is required.")
            .MaximumLength(MaxUrlLength).WithMessage($"Image URL cannot exceed {MaxUrlLength} characters.");

        RuleFor(x => x.AltText)
            .MaximumLength(MaxAltTextLength).WithMessage($"Alt text cannot exceed {MaxAltTextLength} characters.")
            .When(x => x.AltText is not null);

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Image sort order must be a non-negative number.");
    }
}
