namespace NOIR.Application.Features.Products.Commands.DeleteProductCategory;

/// <summary>
/// Validator for DeleteProductCategoryCommand.
/// </summary>
public sealed class DeleteProductCategoryCommandValidator : AbstractValidator<DeleteProductCategoryCommand>
{
    public DeleteProductCategoryCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Category ID is required.");
    }
}
