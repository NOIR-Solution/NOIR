namespace NOIR.Application.Features.Products.Commands.AddProductOption;

/// <summary>
/// Wolverine handler for adding an option to a product.
/// </summary>
public class AddProductOptionCommandHandler
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public AddProductOptionCommandHandler(
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

    public async Task<Result<ProductOptionDto>> Handle(
        AddProductOptionCommand command,
        CancellationToken cancellationToken)
    {
        // Get product with tracking and options loaded (optimized - no variants/images)
        var productSpec = new ProductByIdForOptionUpdateSpec(command.ProductId);
        var product = await _productRepository.FirstOrDefaultAsync(productSpec, cancellationToken);

        if (product is null)
        {
            return Result.Failure<ProductOptionDto>(
                Error.NotFound($"Product with ID '{command.ProductId}' not found.", "NOIR-PRODUCT-021"));
        }

        // Check if option with same name already exists
        if (product.Options.Any(o => o.Name == command.Name.ToLowerInvariant().Replace(" ", "_")))
        {
            return Result.Failure<ProductOptionDto>(
                Error.Validation("Name", $"Option '{command.Name}' already exists for this product.", "NOIR-PRODUCT-050"));
        }

        // Add option to product
        var option = product.AddOption(command.Name, command.DisplayName);
        _unitOfWork.TrackAsAdded(option);

        // Add values if provided
        if (command.Values is { Count: > 0 })
        {
            foreach (var valueReq in command.Values)
            {
                var optionValue = option.AddValue(valueReq.Value, valueReq.DisplayValue);
                _unitOfWork.TrackAsAdded(optionValue);
                if (valueReq.ColorCode is not null)
                {
                    optionValue.SetColorCode(valueReq.ColorCode);
                }
                if (valueReq.SwatchUrl is not null)
                {
                    optionValue.SetSwatchUrl(valueReq.SwatchUrl);
                }
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "Product",
            entityId: product.Id,
            operation: EntityOperation.Updated,
            tenantId: _currentUser.TenantId!,
            cancellationToken);

        return Result.Success(ProductMapper.ToDto(option));
    }
}
