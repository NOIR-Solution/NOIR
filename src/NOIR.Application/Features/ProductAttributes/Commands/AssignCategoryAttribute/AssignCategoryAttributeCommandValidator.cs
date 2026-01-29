namespace NOIR.Application.Features.ProductAttributes.Commands.AssignCategoryAttribute;

/// <summary>
/// Validator for AssignCategoryAttributeCommand.
/// </summary>
public class AssignCategoryAttributeCommandValidator : AbstractValidator<AssignCategoryAttributeCommand>
{
    public AssignCategoryAttributeCommandValidator()
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category ID is required.");

        RuleFor(x => x.AttributeId)
            .NotEmpty().WithMessage("Attribute ID is required.");

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Sort order must be non-negative.");
    }
}
