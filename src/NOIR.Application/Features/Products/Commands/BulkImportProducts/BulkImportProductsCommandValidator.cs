namespace NOIR.Application.Features.Products.Commands.BulkImportProducts;

public class BulkImportProductsCommandValidator : AbstractValidator<BulkImportProductsCommand>
{
    public BulkImportProductsCommandValidator()
    {
        RuleFor(x => x.Products)
            .NotEmpty()
            .WithMessage("At least one product is required.");

        RuleFor(x => x.Products.Count)
            .LessThanOrEqualTo(ProductConstants.MaxBulkOperationSize)
            .WithMessage($"Maximum {ProductConstants.MaxBulkOperationSize} products per import.");

        RuleForEach(x => x.Products).ChildRules(product =>
        {
            product.RuleFor(p => p.Name)
                .NotEmpty()
                .WithMessage("Product name is required.")
                .MaximumLength(200)
                .WithMessage("Product name must not exceed 200 characters.");

            product.RuleFor(p => p.BasePrice)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Base price must be 0 or greater.");
        });
    }
}
