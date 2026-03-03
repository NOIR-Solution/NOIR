namespace NOIR.Application.Features.Products.Commands.DeleteProductImage;

/// <summary>
/// Wolverine handler for deleting a product image.
/// </summary>
public class DeleteProductImageCommandHandler
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public DeleteProductImageCommandHandler(
        IRepository<Product, Guid> productRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IEntityUpdateHubContext entityUpdateHub)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _entityUpdateHub = entityUpdateHub;
    }

    public async Task<Result<bool>> Handle(
        DeleteProductImageCommand command,
        CancellationToken cancellationToken)
    {
        // Get product with tracking and images loaded
        var productSpec = new ProductByIdForUpdateSpec(command.ProductId);
        var product = await _productRepository.FirstOrDefaultAsync(productSpec, cancellationToken);

        if (product is null)
        {
            return Result.Failure<bool>(
                Error.NotFound($"Product with ID '{command.ProductId}' not found.", "NOIR-PRODUCT-029"));
        }

        // Find the image
        var image = product.Images.FirstOrDefault(i => i.Id == command.ImageId);
        if (image is null)
        {
            return Result.Failure<bool>(
                Error.NotFound($"Image with ID '{command.ImageId}' not found.", "NOIR-PRODUCT-030"));
        }

        // Remove image from product
        product.RemoveImage(command.ImageId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "Product",
            entityId: product.Id,
            operation: EntityOperation.Updated,
            tenantId: _currentUser.TenantId!,
            cancellationToken);

        return Result.Success(true);
    }
}
