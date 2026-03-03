namespace NOIR.Application.Features.ProductAttributes.Commands.DeleteProductAttribute;

/// <summary>
/// Wolverine handler for deleting a product attribute.
/// Validates FK constraints before deletion to provide user-friendly error messages.
/// </summary>
public class DeleteProductAttributeCommandHandler
{
    private readonly IRepository<ProductAttribute, Guid> _attributeRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public DeleteProductAttributeCommandHandler(
        IRepository<ProductAttribute, Guid> attributeRepository,
        IApplicationDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IEntityUpdateHubContext entityUpdateHub)
    {
        _attributeRepository = attributeRepository;
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _entityUpdateHub = entityUpdateHub;
    }

    public async Task<Result<bool>> Handle(
        DeleteProductAttributeCommand command,
        CancellationToken cancellationToken)
    {
        // Find the attribute with tracking enabled for modification
        var spec = new ProductAttributeByIdForUpdateSpec(command.Id);
        var attribute = await _attributeRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (attribute == null)
        {
            return Result.Failure<bool>(
                Error.NotFound($"Product attribute with ID '{command.Id}' not found.", ErrorCodes.Attribute.NotFound));
        }

        // Validate: Check for product assignments (FK constraint with Restrict)
        var hasAssignments = await _dbContext.ProductAttributeAssignments
            .TagWith("DeleteProductAttribute.CheckAssignments")
            .AnyAsync(paa => paa.AttributeId == command.Id, cancellationToken);

        if (hasAssignments)
        {
            return Result.Failure<bool>(
                Error.Validation(
                    nameof(command.Id),
                    $"Cannot delete attribute '{attribute.Name}' because it is assigned to products. Remove the attribute from products first.",
                    ErrorCodes.Attribute.HasProducts));
        }

        // Validate: Check for category links (FK constraint with Restrict)
        var categoryLinks = await _dbContext.CategoryAttributes
            .TagWith("DeleteProductAttribute.CheckCategoryLinks")
            .Where(ca => ca.AttributeId == command.Id)
            .Include(ca => ca.Category)
            .ToListAsync(cancellationToken);

        if (categoryLinks.Count > 0)
        {
            var categoryNames = string.Join(", ", categoryLinks.Take(3).Select(ca => ca.Category.Name));
            var suffix = categoryLinks.Count > 3 ? $" and {categoryLinks.Count - 3} more" : "";

            return Result.Failure<bool>(
                Error.Validation(
                    nameof(command.Id),
                    $"Cannot delete attribute '{attribute.Name}' because it is assigned to {categoryLinks.Count} categories: {categoryNames}{suffix}. Remove the attribute from these categories first.",
                    ErrorCodes.Attribute.HasCategories));
        }

        // Set the name for audit logging
        command.AttributeName = attribute.Name;

        // Mark as deleted (domain event)
        attribute.MarkAsDeleted();

        // Soft delete via repository (handled by interceptor)
        _attributeRepository.Remove(attribute);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "ProductAttribute",
            entityId: attribute.Id,
            operation: EntityOperation.Deleted,
            tenantId: _currentUser.TenantId!,
            cancellationToken);

        return Result.Success(true);
    }
}
