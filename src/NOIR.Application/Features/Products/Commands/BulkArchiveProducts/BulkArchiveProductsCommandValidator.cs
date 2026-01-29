namespace NOIR.Application.Features.Products.Commands.BulkArchiveProducts;

public class BulkArchiveProductsCommandValidator : AbstractValidator<BulkArchiveProductsCommand>
{
    public BulkArchiveProductsCommandValidator()
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
