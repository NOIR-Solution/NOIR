namespace NOIR.Application.Features.ProductAttributes.Commands.AssignCategoryAttribute;

/// <summary>
/// Wolverine handler for assigning an attribute to a category.
/// </summary>
public class AssignCategoryAttributeCommandHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IRepository<ProductCategory, Guid> _categoryRepository;
    private readonly IRepository<ProductAttribute, Guid> _attributeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AssignCategoryAttributeCommandHandler(
        IApplicationDbContext dbContext,
        IRepository<ProductCategory, Guid> categoryRepository,
        IRepository<ProductAttribute, Guid> attributeRepository,
        IUnitOfWork unitOfWork)
    {
        _dbContext = dbContext;
        _categoryRepository = categoryRepository;
        _attributeRepository = attributeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CategoryAttributeDto>> Handle(
        AssignCategoryAttributeCommand command,
        CancellationToken cancellationToken)
    {
        // Verify category exists
        var category = await _categoryRepository.GetByIdAsync(command.CategoryId, cancellationToken);
        if (category == null)
        {
            return Result.Failure<CategoryAttributeDto>(
                Error.NotFound($"Category with ID '{command.CategoryId}' not found.", "NOIR-PRODUCT-003"));
        }

        // Verify attribute exists
        var attribute = await _attributeRepository.GetByIdAsync(command.AttributeId, cancellationToken);
        if (attribute == null)
        {
            return Result.Failure<CategoryAttributeDto>(
                Error.NotFound($"Attribute with ID '{command.AttributeId}' not found.", ErrorCodes.Attribute.NotFound));
        }

        // Set names for audit
        command.CategoryName = category.Name;
        command.AttributeName = attribute.Name;

        // Check if link already exists
        var existingLink = await _dbContext.CategoryAttributes
            .FirstOrDefaultAsync(ca => ca.CategoryId == command.CategoryId && ca.AttributeId == command.AttributeId, cancellationToken);

        if (existingLink != null)
        {
            return Result.Failure<CategoryAttributeDto>(
                Error.Conflict($"Attribute '{attribute.Name}' is already linked to category '{category.Name}'.", ErrorCodes.Attribute.AlreadyLinkedToCategory));
        }

        // Create the link
        var categoryAttribute = CategoryAttribute.Create(
            command.CategoryId,
            command.AttributeId,
            command.IsRequired,
            command.SortOrder,
            category.TenantId);

        _dbContext.CategoryAttributes.Add(categoryAttribute);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Re-fetch with navigation properties for DTO
        var result = await _dbContext.CategoryAttributes
            .Include(ca => ca.Category)
            .Include(ca => ca.Attribute)
            .FirstOrDefaultAsync(ca => ca.Id == categoryAttribute.Id, cancellationToken);

        return Result.Success(ProductAttributeMapper.ToCategoryAttributeDto(result!));
    }
}
