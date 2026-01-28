namespace NOIR.Application.Features.Products.Commands.DeleteProductOption;

/// <summary>
/// Validator for DeleteProductOptionCommand.
/// </summary>
public sealed class DeleteProductOptionCommandValidator : AbstractValidator<DeleteProductOptionCommand>
{
    public DeleteProductOptionCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required.");

        RuleFor(x => x.OptionId)
            .NotEmpty().WithMessage("Option ID is required.");
    }
}
