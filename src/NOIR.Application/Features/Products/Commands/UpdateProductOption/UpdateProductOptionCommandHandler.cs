namespace NOIR.Application.Features.Products.Commands.UpdateProductOption;

/// <summary>
/// Wolverine handler for updating a product option.
/// </summary>
public class UpdateProductOptionCommandHandler
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductOptionCommandHandler(
        IRepository<Product, Guid> productRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProductOptionDto>> Handle(
        UpdateProductOptionCommand command,
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

        // Find the option
        var option = product.Options.FirstOrDefault(o => o.Id == command.OptionId);
        if (option is null)
        {
            return Result.Failure<ProductOptionDto>(
                Error.NotFound($"Option with ID '{command.OptionId}' not found.", "NOIR-PRODUCT-051"));
        }

        // Check if another option with the same name exists
        var normalizedName = command.Name.ToLowerInvariant().Replace(" ", "_");
        if (product.Options.Any(o => o.Id != command.OptionId && o.Name == normalizedName))
        {
            return Result.Failure<ProductOptionDto>(
                Error.Validation("Name", $"Option '{command.Name}' already exists for this product.", "NOIR-PRODUCT-050"));
        }

        // Update the option
        option.Update(command.Name, command.DisplayName, command.SortOrder);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(ProductMapper.ToDto(option));
    }
}
