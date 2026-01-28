namespace NOIR.Application.Features.Products.Commands.ReorderProductImages;

/// <summary>
/// Validator for ReorderProductImagesCommand.
/// </summary>
public sealed class ReorderProductImagesCommandValidator : AbstractValidator<ReorderProductImagesCommand>
{
    public ReorderProductImagesCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("At least one image must be provided for reordering.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ImageId)
                .NotEmpty().WithMessage("Image ID is required.");

            item.RuleFor(i => i.SortOrder)
                .GreaterThanOrEqualTo(0).WithMessage("Sort order must be a non-negative number.");
        });
    }
}
