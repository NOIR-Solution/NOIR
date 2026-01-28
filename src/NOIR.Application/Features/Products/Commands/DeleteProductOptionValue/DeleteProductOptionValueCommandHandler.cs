namespace NOIR.Application.Features.Products.Commands.DeleteProductOptionValue;

/// <summary>
/// Wolverine handler for deleting a product option value.
/// </summary>
public class DeleteProductOptionValueCommandHandler
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProductOptionValueCommandHandler(
        IRepository<Product, Guid> productRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(
        DeleteProductOptionValueCommand command,
        CancellationToken cancellationToken)
    {
        // Get product with tracking and options loaded (optimized - no variants/images)
        var productSpec = new ProductByIdForOptionUpdateSpec(command.ProductId);
        var product = await _productRepository.FirstOrDefaultAsync(productSpec, cancellationToken);

        if (product is null)
        {
            return Result.Failure<bool>(
                Error.NotFound($"Product with ID '{command.ProductId}' not found.", "NOIR-PRODUCT-021"));
        }

        // Find the option
        var option = product.Options.FirstOrDefault(o => o.Id == command.OptionId);
        if (option is null)
        {
            return Result.Failure<bool>(
                Error.NotFound($"Option with ID '{command.OptionId}' not found.", "NOIR-PRODUCT-051"));
        }

        // Find and remove the value
        var optionValue = option.Values.FirstOrDefault(v => v.Id == command.ValueId);
        if (optionValue is null)
        {
            return Result.Failure<bool>(
                Error.NotFound($"Option value with ID '{command.ValueId}' not found.", "NOIR-PRODUCT-053"));
        }

        option.RemoveValue(command.ValueId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
