namespace NOIR.Application.Features.ProductAttributes.Commands.BulkUpdateProductAttributes;

/// <summary>
/// Validator for BulkUpdateProductAttributesCommand.
/// </summary>
public class BulkUpdateProductAttributesCommandValidator : AbstractValidator<BulkUpdateProductAttributesCommand>
{
    public BulkUpdateProductAttributesCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Product ID is required.");

        RuleFor(x => x.Values)
            .NotNull()
            .WithMessage("Values collection is required.");

        RuleForEach(x => x.Values)
            .ChildRules(value =>
            {
                value.RuleFor(v => v.AttributeId)
                    .NotEmpty()
                    .WithMessage("Attribute ID is required for each value.");
            });
    }
}
