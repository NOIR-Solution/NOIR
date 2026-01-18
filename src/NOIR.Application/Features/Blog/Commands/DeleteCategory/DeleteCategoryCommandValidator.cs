namespace NOIR.Application.Features.Blog.Commands.DeleteCategory;

/// <summary>
/// Validator for DeleteCategoryCommand.
/// </summary>
public sealed class DeleteCategoryCommandValidator : AbstractValidator<DeleteCategoryCommand>
{
    public DeleteCategoryCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Category ID is required.");
    }
}
