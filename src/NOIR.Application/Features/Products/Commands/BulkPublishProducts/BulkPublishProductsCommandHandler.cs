namespace NOIR.Application.Features.Products.Commands.BulkPublishProducts;

/// <summary>
/// Wolverine handler for bulk publishing products.
/// </summary>
public class BulkPublishProductsCommandHandler
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public BulkPublishProductsCommandHandler(
        IRepository<Product, Guid> productRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<BulkOperationResultDto>> Handle(
        BulkPublishProductsCommand command,
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

            if (product.Status != ProductStatus.Draft)
            {
                errors.Add(new BulkOperationErrorDto(productId, product.Name, $"Product is not in Draft status (current: {product.Status})"));
                continue;
            }

            try
            {
                product.Publish();
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
