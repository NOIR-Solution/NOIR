namespace NOIR.Application.Features.ProductAttributes.Commands.RemoveCategoryAttribute;

/// <summary>
/// Wolverine handler for removing an attribute from a category.
/// </summary>
public class RemoveCategoryAttributeCommandHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveCategoryAttributeCommandHandler(
        IApplicationDbContext dbContext,
        IUnitOfWork unitOfWork)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(
        RemoveCategoryAttributeCommand command,
        CancellationToken cancellationToken)
    {
        // Find the link with includes for audit
        var categoryAttribute = await _dbContext.CategoryAttributes
            .Include(ca => ca.Category)
            .Include(ca => ca.Attribute)
            .FirstOrDefaultAsync(ca => ca.CategoryId == command.CategoryId && ca.AttributeId == command.AttributeId, cancellationToken);

        if (categoryAttribute == null)
        {
            return Result.Failure<bool>(
                Error.NotFound($"Category-attribute link not found for category '{command.CategoryId}' and attribute '{command.AttributeId}'.", ErrorCodes.Attribute.CategoryLinkNotFound));
        }

        // Set names for audit
        command.CategoryName = categoryAttribute.Category?.Name;
        command.AttributeName = categoryAttribute.Attribute?.Name;

        _dbContext.CategoryAttributes.Remove(categoryAttribute);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
