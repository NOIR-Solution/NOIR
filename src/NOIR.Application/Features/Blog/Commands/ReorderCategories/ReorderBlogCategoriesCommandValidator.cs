namespace NOIR.Application.Features.Blog.Commands.ReorderCategories;

/// <summary>
/// Validator for ReorderBlogCategoriesCommand.
/// </summary>
public sealed class ReorderBlogCategoriesCommandValidator : AbstractValidator<ReorderBlogCategoriesCommand>
{
    public ReorderBlogCategoriesCommandValidator()
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
