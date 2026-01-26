namespace NOIR.Application.Features.Products.Commands.AddProductImage;

/// <summary>
/// Validator for AddProductImageCommand.
/// </summary>
public sealed class AddProductImageCommandValidator : AbstractValidator<AddProductImageCommand>
{
    private const int MaxUrlLength = 500;
    private const int MaxAltTextLength = 200;

    public AddProductImageCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required.");

        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("Image URL is required.")
            .MaximumLength(MaxUrlLength).WithMessage($"Image URL cannot exceed {MaxUrlLength} characters.");

        RuleFor(x => x.AltText)
            .MaximumLength(MaxAltTextLength).WithMessage($"Alt text cannot exceed {MaxAltTextLength} characters.")
            .When(x => x.AltText is not null);

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Sort order must be non-negative.");
    }
}
