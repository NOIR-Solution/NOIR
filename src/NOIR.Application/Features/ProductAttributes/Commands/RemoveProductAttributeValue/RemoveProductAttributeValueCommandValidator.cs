namespace NOIR.Application.Features.ProductAttributes.Commands.RemoveProductAttributeValue;

/// <summary>
/// Validator for RemoveProductAttributeValueCommand.
/// </summary>
public class RemoveProductAttributeValueCommandValidator : AbstractValidator<RemoveProductAttributeValueCommand>
{
    public RemoveProductAttributeValueCommandValidator()
    {
        RuleFor(x => x.AttributeId)
            .NotEmpty().WithMessage("Attribute ID is required.");

        RuleFor(x => x.ValueId)
            .NotEmpty().WithMessage("Value ID is required.");
    }
}
