namespace NOIR.Application.Features.ProductAttributes.Commands.DeleteProductAttribute;

/// <summary>
/// Validator for DeleteProductAttributeCommand.
/// </summary>
public class DeleteProductAttributeCommandValidator : AbstractValidator<DeleteProductAttributeCommand>
{
    public DeleteProductAttributeCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Attribute ID is required.");
    }
}
