namespace NOIR.Application.Features.Blog.Commands.UpdateCategory;

/// <summary>
/// Validator for UpdateCategoryCommand.
/// </summary>
public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    private const int MaxNameLength = 200;
    private const int MaxSlugLength = 200;
    private const int MaxDescriptionLength = 1000;
    private const int MaxMetaTitleLength = 200;
    private const int MaxMetaDescriptionLength = 500;
    private const int MaxUrlLength = 2000;

    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Category ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(MaxNameLength).WithMessage($"Name cannot exceed {MaxNameLength} characters.");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required.")
            .MaximumLength(MaxSlugLength).WithMessage($"Slug cannot exceed {MaxSlugLength} characters.")
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$").WithMessage("Slug must be lowercase and contain only letters, numbers, and hyphens.");

        RuleFor(x => x.Description)
            .MaximumLength(MaxDescriptionLength).WithMessage($"Description cannot exceed {MaxDescriptionLength} characters.")
            .When(x => x.Description is not null);

        RuleFor(x => x.MetaTitle)
            .MaximumLength(MaxMetaTitleLength).WithMessage($"Meta title cannot exceed {MaxMetaTitleLength} characters.")
            .When(x => x.MetaTitle is not null);

        RuleFor(x => x.MetaDescription)
            .MaximumLength(MaxMetaDescriptionLength).WithMessage($"Meta description cannot exceed {MaxMetaDescriptionLength} characters.")
            .When(x => x.MetaDescription is not null);

        RuleFor(x => x.ImageUrl)
            .MaximumLength(MaxUrlLength).WithMessage($"Image URL cannot exceed {MaxUrlLength} characters.")
            .When(x => x.ImageUrl is not null);

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Sort order must be a non-negative number.");
    }
}
