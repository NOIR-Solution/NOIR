namespace NOIR.Application.Features.Products.Commands.UpdateProductCategory;

/// <summary>
/// Wolverine handler for updating an existing product category.
/// </summary>
public class UpdateProductCategoryCommandHandler
{
    private readonly IRepository<ProductCategory, Guid> _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateProductCategoryCommandHandler(
        IRepository<ProductCategory, Guid> categoryRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<ProductCategoryDto>> Handle(
        UpdateProductCategoryCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        // Get category with tracking
        var categorySpec = new ProductCategoryByIdForUpdateSpec(command.Id);
        var category = await _categoryRepository.FirstOrDefaultAsync(categorySpec, cancellationToken);

        if (category is null)
        {
            return Result.Failure<ProductCategoryDto>(
                Error.NotFound($"Product category with ID '{command.Id}' not found.", "NOIR-PRODUCT-003"));
        }

        // Check if slug changed and is unique
        if (category.Slug != command.Slug.ToLowerInvariant())
        {
            var slugSpec = new ProductCategorySlugExistsSpec(command.Slug, tenantId, command.Id);
            var existingCategory = await _categoryRepository.FirstOrDefaultAsync(slugSpec, cancellationToken);
            if (existingCategory != null)
            {
                return Result.Failure<ProductCategoryDto>(
                    Error.Conflict($"A product category with slug '{command.Slug}' already exists.", "NOIR-PRODUCT-001"));
            }
        }

        // Validate parent exists if specified
        string? parentName = null;
        if (command.ParentId.HasValue)
        {
            // Prevent circular reference
            if (command.ParentId == command.Id)
            {
                return Result.Failure<ProductCategoryDto>(
                    Error.Validation("ParentId", "A category cannot be its own parent.", "NOIR-PRODUCT-004"));
            }

            var parentSpec = new ProductCategoryByIdSpec(command.ParentId.Value);
            var parent = await _categoryRepository.FirstOrDefaultAsync(parentSpec, cancellationToken);
            if (parent is null)
            {
                return Result.Failure<ProductCategoryDto>(
                    Error.NotFound($"Parent category with ID '{command.ParentId}' not found.", "NOIR-PRODUCT-002"));
            }
            parentName = parent.Name;
        }

        // Update category
        category.UpdateDetails(
            command.Name,
            command.Slug,
            command.Description,
            command.ImageUrl);

        category.UpdateSeo(command.MetaTitle, command.MetaDescription);
        category.SetParent(command.ParentId);
        category.SetSortOrder(command.SortOrder);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(ProductMapper.ToDto(category, parentName));
    }
}
