namespace NOIR.Application.Features.Products.Commands.BulkArchiveProducts;

/// <summary>
/// Wolverine handler for bulk archiving products.
/// </summary>
public class BulkArchiveProductsCommandHandler
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public BulkArchiveProductsCommandHandler(
        IRepository<Product, Guid> productRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<BulkOperationResultDto>> Handle(
        BulkArchiveProductsCommand command,
        CancellationToken cancellationToken)
    {
        var successCount = 0;
        var errors = new List<BulkOperationErrorDto>();

        var spec = new ProductsByIdsForUpdateSpec(command.ProductIds);
        var products = await _productRepository.ListAsync(spec, cancellationToken);

        foreach (var productId in command.ProductIds)
        {
            var product = products.FirstOrDefault(p => p.Id == productId);

            if (product is null)
            {
                errors.Add(new BulkOperationErrorDto(productId, null, "Product not found"));
                continue;
            }

            if (product.Status != ProductStatus.Active)
            {
                errors.Add(new BulkOperationErrorDto(productId, product.Name, $"Product is not Active (current: {product.Status})"));
                continue;
            }

            try
            {
                product.Archive();
                successCount++;
            }
            catch (Exception ex)
            {
                errors.Add(new BulkOperationErrorDto(productId, product.Name, ex.Message));
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new BulkOperationResultDto(
            successCount,
            errors.Count,
            errors));
    }
}
