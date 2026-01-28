namespace NOIR.Application.Features.Products.Commands.AddProductOptionValue;

/// <summary>
/// Wolverine handler for adding a value to a product option.
/// </summary>
public class AddProductOptionValueCommandHandler
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddProductOptionValueCommandHandler(
        IRepository<Product, Guid> productRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProductOptionValueDto>> Handle(
        AddProductOptionValueCommand command,
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

        // Check if value already exists
        var normalizedValue = command.Value.ToLowerInvariant().Replace(" ", "_");
        if (option.Values.Any(v => v.Value == normalizedValue))
        {
            return Result.Failure<ProductOptionValueDto>(
                Error.Validation("Value", $"Value '{command.Value}' already exists for this option.", "NOIR-PRODUCT-052"));
        }

        // Add value to option
        var optionValue = option.AddValue(command.Value, command.DisplayValue);

        if (command.ColorCode is not null)
        {
            optionValue.SetColorCode(command.ColorCode);
        }
        if (command.SwatchUrl is not null)
        {
            optionValue.SetSwatchUrl(command.SwatchUrl);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(ProductMapper.ToDto(optionValue));
    }
}
