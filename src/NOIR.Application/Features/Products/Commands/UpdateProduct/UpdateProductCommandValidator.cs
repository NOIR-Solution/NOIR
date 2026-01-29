namespace NOIR.Application.Features.Products.Commands.UpdateProduct;

/// <summary>
/// Validator for UpdateProductCommand.
/// </summary>
public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
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

    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Product ID is required.");

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

        RuleFor(x => x.MetaTitle)
            .MaximumLength(MaxMetaTitleLength).WithMessage($"Meta title cannot exceed {MaxMetaTitleLength} characters.")
            .When(x => x.MetaTitle is not null);

        RuleFor(x => x.MetaDescription)
            .MaximumLength(MaxMetaDescriptionLength).WithMessage($"Meta description cannot exceed {MaxMetaDescriptionLength} characters.")
            .When(x => x.MetaDescription is not null);

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Sort order must be a non-negative number.");
    }
}
