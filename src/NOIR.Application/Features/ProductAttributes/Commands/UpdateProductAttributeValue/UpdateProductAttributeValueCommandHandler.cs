namespace NOIR.Application.Features.ProductAttributes.Commands.UpdateProductAttributeValue;

/// <summary>
/// Wolverine handler for updating a product attribute value.
/// </summary>
public class UpdateProductAttributeValueCommandHandler
{
    private readonly IRepository<ProductAttribute, Guid> _attributeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public UpdateProductAttributeValueCommandHandler(
        IRepository<ProductAttribute, Guid> attributeRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IEntityUpdateHubContext entityUpdateHub)
    {
        _attributeRepository = attributeRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _entityUpdateHub = entityUpdateHub;
    }

    public async Task<Result<ProductAttributeValueDto>> Handle(
        UpdateProductAttributeValueCommand command,
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

        // Find the value
        var value = attribute.GetValue(command.ValueId);
        if (value == null)
        {
            return Result.Failure<ProductAttributeValueDto>(
                Error.NotFound($"Attribute value with ID '{command.ValueId}' not found.", ErrorCodes.Attribute.ValueNotFound));
        }

        // Update the value
        value.UpdateValue(command.Value, command.DisplayValue);
        value.SetVisualDisplay(command.ColorCode, command.SwatchUrl, command.IconUrl);
        value.SetSortOrder(command.SortOrder);
        value.SetActive(command.IsActive);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "ProductAttribute",
            entityId: attribute.Id,
            operation: EntityOperation.Updated,
            tenantId: _currentUser.TenantId!,
            cancellationToken);

        return Result.Success(ProductAttributeMapper.ToValueDto(value));
    }
}
