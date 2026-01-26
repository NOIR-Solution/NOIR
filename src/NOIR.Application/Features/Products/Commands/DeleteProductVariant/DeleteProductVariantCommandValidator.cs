namespace NOIR.Application.Features.Products.Commands.DeleteProductVariant;

/// <summary>
/// Validator for DeleteProductVariantCommand.
/// </summary>
public sealed class DeleteProductVariantCommandValidator : AbstractValidator<DeleteProductVariantCommand>
{
    public DeleteProductVariantCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required.");

        RuleFor(x => x.VariantId)
            .NotEmpty().WithMessage("Variant ID is required.");
    }
}
