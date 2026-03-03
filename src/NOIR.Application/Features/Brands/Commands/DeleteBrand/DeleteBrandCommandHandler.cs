namespace NOIR.Application.Features.Brands.Commands.DeleteBrand;

/// <summary>
/// Wolverine handler for deleting a brand.
/// </summary>
public class DeleteBrandCommandHandler
{
    private readonly IRepository<Brand, Guid> _brandRepository;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public DeleteBrandCommandHandler(
        IRepository<Brand, Guid> brandRepository,
        IRepository<Product, Guid> productRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IEntityUpdateHubContext entityUpdateHub)
    {
        _brandRepository = brandRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _entityUpdateHub = entityUpdateHub;
    }

    public async Task<Result<bool>> Handle(
        DeleteBrandCommand command,
        CancellationToken cancellationToken)
    {
        // Get the brand for update (need tracking for soft delete)
        var spec = new BrandByIdForUpdateSpec(command.Id);
        var brand = await _brandRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (brand == null)
        {
            return Result.Failure<bool>(
                Error.NotFound($"Brand with ID '{command.Id}' was not found.", ErrorCodes.Brand.NotFound));
        }

        // Check if brand has products (efficient EXISTS query)
        var hasProductsSpec = new BrandHasProductsSpec(command.Id);
        var hasProducts = await _productRepository.AnyAsync(hasProductsSpec, cancellationToken);

        if (hasProducts)
        {
            return Result.Failure<bool>(
                Error.Conflict("Cannot delete brand that has associated products.", ErrorCodes.Brand.HasProducts));
        }

        // Raise domain event and soft delete the brand (handled by interceptor)
        brand.MarkAsDeleted();
        _brandRepository.Remove(brand);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "Brand",
            entityId: brand.Id,
            operation: EntityOperation.Deleted,
            tenantId: _currentUser.TenantId!,
            cancellationToken);

        return Result.Success(true);
    }
}
