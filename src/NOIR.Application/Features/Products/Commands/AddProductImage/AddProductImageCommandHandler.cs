namespace NOIR.Application.Features.Products.Commands.AddProductImage;

/// <summary>
/// Wolverine handler for adding an image to a product.
/// </summary>
public class AddProductImageCommandHandler
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public AddProductImageCommandHandler(
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

    public async Task<Result<ProductImageDto>> Handle(
        AddProductImageCommand command,
        CancellationToken cancellationToken)
    {
        // Get product with tracking and images loaded (NOT variants to avoid concurrency token issues)
        var productSpec = new ProductByIdForImageUpdateSpec(command.ProductId);
        var product = await _productRepository.FirstOrDefaultAsync(productSpec, cancellationToken);

        if (product is null)
        {
            return Result.Failure<ProductImageDto>(
                Error.NotFound($"Product with ID '{command.ProductId}' not found.", "NOIR-PRODUCT-026"));
        }

        // TWO-SAVE PATTERN: Add image without isPrimary first to avoid ClearPrimary() causing
        // DbUpdateConcurrencyException when Variants are loaded (they have StockQuantity as concurrency token)
        var isPrimaryRequested = command.IsPrimary;
        var image = product.AddImage(command.Url, command.AltText, isPrimary: false);
        image.SetSortOrder(command.SortOrder);
        _unitOfWork.TrackAsAdded(image);

        // First save: adds the new image only (no modifications to existing entities)
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Second save: if primary was requested, set it now (entities are in clean state)
        if (isPrimaryRequested)
        {
            product.SetPrimaryImage(image.Id);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "Product",
            entityId: product.Id,
            operation: EntityOperation.Updated,
            tenantId: _currentUser.TenantId!,
            cancellationToken);

        return Result.Success(ProductMapper.ToDto(image));
    }
}
