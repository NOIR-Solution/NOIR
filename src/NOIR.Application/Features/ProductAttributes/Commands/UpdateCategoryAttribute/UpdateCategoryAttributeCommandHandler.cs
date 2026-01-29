namespace NOIR.Application.Features.ProductAttributes.Commands.UpdateCategoryAttribute;

/// <summary>
/// Wolverine handler for updating a category-attribute link settings.
/// </summary>
public class UpdateCategoryAttributeCommandHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCategoryAttributeCommandHandler(
        IApplicationDbContext dbContext,
        IUnitOfWork unitOfWork)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CategoryAttributeDto>> Handle(
        UpdateCategoryAttributeCommand command,
        CancellationToken cancellationToken)
    {
        // Find the link with tracking and navigation
        var categoryAttribute = await _dbContext.CategoryAttributes
            .Include(ca => ca.Category)
            .Include(ca => ca.Attribute)
            .FirstOrDefaultAsync(ca => ca.CategoryId == command.CategoryId && ca.AttributeId == command.AttributeId, cancellationToken);

        if (categoryAttribute == null)
        {
            return Result.Failure<CategoryAttributeDto>(
                Error.NotFound($"Category-attribute link not found for category '{command.CategoryId}' and attribute '{command.AttributeId}'.", ErrorCodes.Attribute.CategoryLinkNotFound));
        }

        // Set names for audit
        command.CategoryName = categoryAttribute.Category?.Name;
        command.AttributeName = categoryAttribute.Attribute?.Name;

        // Attach for tracking and update
        _dbContext.Attach(categoryAttribute);

        // Update the settings
        categoryAttribute.SetRequired(command.IsRequired);
        categoryAttribute.SetSortOrder(command.SortOrder);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Re-fetch with navigation properties for DTO
        var result = await _dbContext.CategoryAttributes
            .Include(ca => ca.Category)
            .Include(ca => ca.Attribute)
            .FirstOrDefaultAsync(ca => ca.Id == categoryAttribute.Id, cancellationToken);

        return Result.Success(ProductAttributeMapper.ToCategoryAttributeDto(result!));
    }
}
