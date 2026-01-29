namespace NOIR.Application.Features.ProductAttributes.Commands.CreateProductAttribute;

/// <summary>
/// Wolverine handler for creating a new product attribute.
/// </summary>
public class CreateProductAttributeCommandHandler
{
    private readonly IRepository<ProductAttribute, Guid> _attributeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateProductAttributeCommandHandler(
        IRepository<ProductAttribute, Guid> attributeRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _attributeRepository = attributeRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<ProductAttributeDto>> Handle(
        CreateProductAttributeCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        // Parse the attribute type
        if (!Enum.TryParse<AttributeType>(command.Type, true, out var attributeType))
        {
            return Result.Failure<ProductAttributeDto>(
                Error.Validation("Type", $"Invalid attribute type '{command.Type}'.", ErrorCodes.Attribute.InvalidValueForType));
        }

        // Check if code already exists
        var codeSpec = new ProductAttributeCodeExistsSpec(command.Code);
        var existingAttribute = await _attributeRepository.FirstOrDefaultAsync(codeSpec, cancellationToken);
        if (existingAttribute != null)
        {
            return Result.Failure<ProductAttributeDto>(
                Error.Conflict($"A product attribute with code '{command.Code}' already exists.", ErrorCodes.Attribute.DuplicateCode));
        }

        // Create the attribute
        var attribute = ProductAttribute.Create(command.Code, command.Name, attributeType, tenantId);

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

        // Set global flag
        attribute.SetGlobal(command.IsGlobal);

        await _attributeRepository.AddAsync(attribute, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(ProductAttributeMapper.ToDto(attribute));
    }
}
