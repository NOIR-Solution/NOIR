using NOIR.Application.Features.Products.Specifications;

namespace NOIR.Application.Features.ProductAttributes.Commands.BulkUpdateProductAttributes;

/// <summary>
/// Wolverine handler for bulk updating a product's attribute values.
/// </summary>
public class BulkUpdateProductAttributesCommandHandler
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IMessageBus _messageBus;

    public BulkUpdateProductAttributesCommandHandler(
        IRepository<Product, Guid> productRepository,
        IMessageBus messageBus)
    {
        _productRepository = productRepository;
        _messageBus = messageBus;
    }

    public async Task<Result<IReadOnlyCollection<ProductAttributeAssignmentDto>>> Handle(
        BulkUpdateProductAttributesCommand command,
        CancellationToken cancellationToken)
    {
        // Verify product exists (with variants loaded for validation)
        var productSpec = new ProductByIdForUpdateSpec(command.ProductId);
        var product = await _productRepository.FirstOrDefaultAsync(productSpec, cancellationToken);
        if (product == null)
        {
            return Result.Failure<IReadOnlyCollection<ProductAttributeAssignmentDto>>(
                Error.NotFound($"Product with ID '{command.ProductId}' not found.", ErrorCodes.Product.NotFound));
        }

        // Set name for audit
        command.ProductName = product.Name;

        // If VariantId is provided, verify it exists on the product
        if (command.VariantId.HasValue)
        {
            var variant = product.Variants.FirstOrDefault(v => v.Id == command.VariantId);
            if (variant == null)
            {
                return Result.Failure<IReadOnlyCollection<ProductAttributeAssignmentDto>>(
                    Error.NotFound($"Variant with ID '{command.VariantId}' not found for this product.", ErrorCodes.Product.VariantNotFound));
            }
        }

        if (command.Values == null || !command.Values.Any())
        {
            return Result.Success<IReadOnlyCollection<ProductAttributeAssignmentDto>>(Array.Empty<ProductAttributeAssignmentDto>());
        }

        var results = new List<ProductAttributeAssignmentDto>();

        foreach (var valueItem in command.Values)
        {
            var setCommand = new SetProductAttributeValue.SetProductAttributeValueCommand(
                command.ProductId,
                valueItem.AttributeId,
                command.VariantId,
                valueItem.Value)
            {
                UserId = command.UserId
            };

            // Use message bus to invoke the individual set command
            var result = await _messageBus.InvokeAsync<Result<ProductAttributeAssignmentDto>>(setCommand, cancellationToken);

            // Fail fast on first error - clearer UX, user can fix and retry
            if (!result.IsSuccess)
            {
                return Result.Failure<IReadOnlyCollection<ProductAttributeAssignmentDto>>(
                    Error.Validation("Values", $"Failed to update attribute: {result.Error.Message}", result.Error.Code));
            }

            if (result.Value != null)
            {
                results.Add(result.Value);
            }
        }

        return Result.Success<IReadOnlyCollection<ProductAttributeAssignmentDto>>(results);
    }
}
