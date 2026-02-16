namespace NOIR.Application.Features.Products.Commands.ReorderProductCategories;

/// <summary>
/// Validator for ReorderProductCategoriesCommand.
/// </summary>
public sealed class ReorderProductCategoriesCommandValidator : AbstractValidator<ReorderProductCategoriesCommand>
{
    public ReorderProductCategoriesCommandValidator()
    {
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("At least one category must be provided for reordering.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.CategoryId)
                .NotEmpty().WithMessage("Category ID is required.");

            item.RuleFor(i => i.SortOrder)
                .GreaterThanOrEqualTo(0).WithMessage("Sort order must be a non-negative number.");
        });
    }
}
