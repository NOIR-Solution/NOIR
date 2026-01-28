namespace NOIR.Application.Features.Products.Commands.UpdateProductOptionValue;

/// <summary>
/// Wolverine handler for updating a product option value.
/// </summary>
public class UpdateProductOptionValueCommandHandler
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductOptionValueCommandHandler(
        IRepository<Product, Guid> productRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProductOptionValueDto>> Handle(
        UpdateProductOptionValueCommand command,
        CancellationToken cancellationToken)
    {
        // Get product with tracking and options loaded (optimized - no variants/images)
        var productSpec = new ProductByIdForOptionUpdateSpec(command.ProductId);
        var product = await _productRepository.FirstOrDefaultAsync(productSpec, cancellationToken);

        if (product is null)
        {
            return Result.Failure<ProductOptionValueDto>(
                Error.NotFound($"Product with ID '{command.ProductId}' not found.", "NOIR-PRODUCT-021"));
        }

        // Find the option
        var option = product.Options.FirstOrDefault(o => o.Id == command.OptionId);
        if (option is null)
        {
            return Result.Failure<ProductOptionValueDto>(
                Error.NotFound($"Option with ID '{command.OptionId}' not found.", "NOIR-PRODUCT-051"));
        }

        // Find the value
        var optionValue = option.Values.FirstOrDefault(v => v.Id == command.ValueId);
        if (optionValue is null)
        {
            return Result.Failure<ProductOptionValueDto>(
                Error.NotFound($"Option value with ID '{command.ValueId}' not found.", "NOIR-PRODUCT-053"));
        }

        // Check if another value with the same name exists
        var normalizedValue = command.Value.ToLowerInvariant().Replace(" ", "_");
        if (option.Values.Any(v => v.Id != command.ValueId && v.Value == normalizedValue))
        {
            return Result.Failure<ProductOptionValueDto>(
                Error.Validation("Value", $"Value '{command.Value}' already exists for this option.", "NOIR-PRODUCT-052"));
        }

        // Update the value
        optionValue.Update(command.Value, command.DisplayValue, command.SortOrder);
        optionValue.SetColorCode(command.ColorCode);
        optionValue.SetSwatchUrl(command.SwatchUrl);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(ProductMapper.ToDto(optionValue));
    }
}
