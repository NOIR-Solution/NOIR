namespace NOIR.Application.Features.ProductAttributes.Commands.RemoveProductAttributeValue;

/// <summary>
/// Wolverine handler for removing a value from a product attribute.
/// </summary>
public class RemoveProductAttributeValueCommandHandler
{
    private readonly IRepository<ProductAttribute, Guid> _attributeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveProductAttributeValueCommandHandler(
        IRepository<ProductAttribute, Guid> attributeRepository,
        IUnitOfWork unitOfWork)
    {
        _attributeRepository = attributeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(
        RemoveProductAttributeValueCommand command,
        CancellationToken cancellationToken)
    {
        // Find the attribute with tracking and values
        var spec = new ProductAttributeByIdForUpdateSpec(command.AttributeId, includeValues: true);
        var attribute = await _attributeRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (attribute == null)
        {
            return Result.Failure<bool>(
                Error.NotFound($"Product attribute with ID '{command.AttributeId}' not found.", ErrorCodes.Attribute.NotFound));
        }

        // Get the value for audit logging
        var value = attribute.GetValue(command.ValueId);
        if (value != null)
        {
            command.ValueDisplayName = value.DisplayValue;
        }

        try
        {
            // Remove the value (domain validates existence)
            attribute.RemoveValue(command.ValueId);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(true);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<bool>(
                Error.NotFound(ex.Message, ErrorCodes.Attribute.ValueNotFound));
        }
    }
}
