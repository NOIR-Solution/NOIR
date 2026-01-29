namespace NOIR.Application.Features.ProductAttributes.Commands.AddProductAttributeValue;

/// <summary>
/// Wolverine handler for adding a value to a product attribute.
/// </summary>
public class AddProductAttributeValueCommandHandler
{
    private readonly IRepository<ProductAttribute, Guid> _attributeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddProductAttributeValueCommandHandler(
        IRepository<ProductAttribute, Guid> attributeRepository,
        IUnitOfWork unitOfWork)
    {
        _attributeRepository = attributeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProductAttributeValueDto>> Handle(
        AddProductAttributeValueCommand command,
        CancellationToken cancellationToken)
    {
        // Find the attribute with tracking and values
        var spec = new ProductAttributeByIdForUpdateSpec(command.AttributeId, includeValues: true);
        var attribute = await _attributeRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (attribute == null)
        {
            return Result.Failure<ProductAttributeValueDto>(
                Error.NotFound($"Product attribute with ID '{command.AttributeId}' not found.", ErrorCodes.Attribute.NotFound));
        }

        try
        {
            // Add the value (domain validates type compatibility)
            var value = attribute.AddValue(command.Value, command.DisplayValue, command.SortOrder);

            // Set visual display if provided
            if (!string.IsNullOrEmpty(command.ColorCode) ||
                !string.IsNullOrEmpty(command.SwatchUrl) ||
                !string.IsNullOrEmpty(command.IconUrl))
            {
                value.SetVisualDisplay(command.ColorCode, command.SwatchUrl, command.IconUrl);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(ProductAttributeMapper.ToValueDto(value));
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<ProductAttributeValueDto>(
                Error.Validation("Value", ex.Message));
        }
    }
}
