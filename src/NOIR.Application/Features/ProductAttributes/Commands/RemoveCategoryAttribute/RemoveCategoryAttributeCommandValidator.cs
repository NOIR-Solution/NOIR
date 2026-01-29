namespace NOIR.Application.Features.ProductAttributes.Commands.RemoveCategoryAttribute;

/// <summary>
/// Validator for RemoveCategoryAttributeCommand.
/// </summary>
public class RemoveCategoryAttributeCommandValidator : AbstractValidator<RemoveCategoryAttributeCommand>
{
    public RemoveCategoryAttributeCommandValidator()
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category ID is required.");

        RuleFor(x => x.AttributeId)
            .NotEmpty().WithMessage("Attribute ID is required.");
    }
}
