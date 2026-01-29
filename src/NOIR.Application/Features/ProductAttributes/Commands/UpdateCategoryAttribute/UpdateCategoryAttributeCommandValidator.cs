namespace NOIR.Application.Features.ProductAttributes.Commands.UpdateCategoryAttribute;

/// <summary>
/// Validator for UpdateCategoryAttributeCommand.
/// </summary>
public class UpdateCategoryAttributeCommandValidator : AbstractValidator<UpdateCategoryAttributeCommand>
{
    public UpdateCategoryAttributeCommandValidator()
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category ID is required.");

        RuleFor(x => x.AttributeId)
            .NotEmpty().WithMessage("Attribute ID is required.");

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Sort order must be non-negative.");
    }
}
