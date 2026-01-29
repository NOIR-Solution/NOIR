namespace NOIR.Application.Features.ProductAttributes.Commands.UpdateProductAttribute;

/// <summary>
/// Wolverine handler for updating an existing product attribute.
/// </summary>
public class UpdateProductAttributeCommandHandler
{
    private readonly IRepository<ProductAttribute, Guid> _attributeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductAttributeCommandHandler(
        IRepository<ProductAttribute, Guid> attributeRepository,
        IUnitOfWork unitOfWork)
    {
        _attributeRepository = attributeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProductAttributeDto>> Handle(
        UpdateProductAttributeCommand command,
        CancellationToken cancellationToken)
    {
        // Find the attribute with tracking enabled for modification
        var spec = new ProductAttributeByIdForUpdateSpec(command.Id, includeValues: true);
        var attribute = await _attributeRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (attribute == null)
        {
            return Result.Failure<ProductAttributeDto>(
                Error.NotFound($"Product attribute with ID '{command.Id}' not found.", ErrorCodes.Attribute.NotFound));
        }

        // Check if code already exists (excluding current)
        var codeSpec = new ProductAttributeCodeExistsSpec(command.Code, command.Id);
        var existingAttribute = await _attributeRepository.FirstOrDefaultAsync(codeSpec, cancellationToken);
        if (existingAttribute != null)
        {
            return Result.Failure<ProductAttributeDto>(
                Error.Conflict($"A product attribute with code '{command.Code}' already exists.", ErrorCodes.Attribute.DuplicateCode));
        }

        // Update the attribute
        attribute.UpdateDetails(command.Code, command.Name);

        // Set behavior flags
        attribute.SetBehaviorFlags(
            command.IsFilterable,
            command.IsSearchable,
            command.IsRequired,
            command.IsVariantAttribute);

        // Set display flags
        attribute.SetDisplayFlags(command.ShowInProductCard, command.ShowInSpecifications);

        // Set type configuration
        attribute.SetTypeConfiguration(
            command.Unit,
            command.ValidationRegex,
            command.MinValue,
            command.MaxValue,
            command.MaxLength);

        // Set defaults
        attribute.SetDefaults(command.DefaultValue, command.Placeholder, command.HelpText);

        // Set organization
        attribute.SetSortOrder(command.SortOrder);
        attribute.SetActive(command.IsActive);

        // Set global flag
        attribute.SetGlobal(command.IsGlobal);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(ProductAttributeMapper.ToDto(attribute));
    }
}
