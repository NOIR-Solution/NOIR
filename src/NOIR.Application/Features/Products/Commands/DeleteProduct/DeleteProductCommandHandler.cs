namespace NOIR.Application.Features.Products.Commands.DeleteProduct;

/// <summary>
/// Wolverine handler for soft-deleting a product.
/// </summary>
public class DeleteProductCommandHandler
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProductCommandHandler(
        IRepository<Product, Guid> productRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(
        DeleteProductCommand command,
        CancellationToken cancellationToken)
    {
        // Get product with tracking
        var productSpec = new ProductByIdForUpdateSpec(command.Id);
        var product = await _productRepository.FirstOrDefaultAsync(productSpec, cancellationToken);

        if (product is null)
        {
            return Result.Failure<bool>(
                Error.NotFound($"Product with ID '{command.Id}' not found.", "NOIR-PRODUCT-020"));
        }

        // Soft delete the product (handled by interceptor)
        _productRepository.Remove(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
