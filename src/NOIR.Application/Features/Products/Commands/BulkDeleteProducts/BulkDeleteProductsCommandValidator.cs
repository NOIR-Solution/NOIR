namespace NOIR.Application.Features.Products.Commands.BulkDeleteProducts;

public class BulkDeleteProductsCommandValidator : AbstractValidator<BulkDeleteProductsCommand>
{
    public BulkDeleteProductsCommandValidator()
    {
        RuleFor(x => x.ProductIds)
            .NotEmpty()
            .WithMessage("At least one product ID is required.");

        RuleFor(x => x.ProductIds.Count)
            .LessThanOrEqualTo(ProductConstants.MaxBulkOperationSize)
            .WithMessage($"Maximum {ProductConstants.MaxBulkOperationSize} products per operation.");

        RuleForEach(x => x.ProductIds)
            .NotEmpty()
            .WithMessage("Product ID cannot be empty.");
    }
}
