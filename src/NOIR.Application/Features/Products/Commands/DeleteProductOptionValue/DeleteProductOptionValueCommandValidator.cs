namespace NOIR.Application.Features.Products.Commands.DeleteProductOptionValue;

/// <summary>
/// Validator for DeleteProductOptionValueCommand.
/// </summary>
public sealed class DeleteProductOptionValueCommandValidator : AbstractValidator<DeleteProductOptionValueCommand>
{
    public DeleteProductOptionValueCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required.");

        RuleFor(x => x.OptionId)
            .NotEmpty().WithMessage("Option ID is required.");

        RuleFor(x => x.ValueId)
            .NotEmpty().WithMessage("Value ID is required.");
    }
}
