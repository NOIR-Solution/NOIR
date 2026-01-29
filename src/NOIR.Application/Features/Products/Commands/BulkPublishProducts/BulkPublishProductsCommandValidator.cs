namespace NOIR.Application.Features.Products.Commands.BulkPublishProducts;

public class BulkPublishProductsCommandValidator : AbstractValidator<BulkPublishProductsCommand>
{
    public BulkPublishProductsCommandValidator()
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
