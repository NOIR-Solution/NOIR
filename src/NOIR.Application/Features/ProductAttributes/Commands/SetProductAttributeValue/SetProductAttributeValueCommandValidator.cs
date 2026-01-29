namespace NOIR.Application.Features.ProductAttributes.Commands.SetProductAttributeValue;

/// <summary>
/// Validator for SetProductAttributeValueCommand.
/// </summary>
public class SetProductAttributeValueCommandValidator : AbstractValidator<SetProductAttributeValueCommand>
{
    public SetProductAttributeValueCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Product ID is required.");

        RuleFor(x => x.AttributeId)
            .NotEmpty()
            .WithMessage("Attribute ID is required.");
    }
}
