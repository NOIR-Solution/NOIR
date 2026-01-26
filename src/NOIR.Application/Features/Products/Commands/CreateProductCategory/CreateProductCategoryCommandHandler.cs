namespace NOIR.Application.Features.Products.Commands.CreateProductCategory;

/// <summary>
/// Wolverine handler for creating a new product category.
/// </summary>
public class CreateProductCategoryCommandHandler
{
    private readonly IRepository<ProductCategory, Guid> _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateProductCategoryCommandHandler(
        IRepository<ProductCategory, Guid> categoryRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<ProductCategoryDto>> Handle(
        CreateProductCategoryCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        // Check if slug already exists
        var slugSpec = new ProductCategorySlugExistsSpec(command.Slug, tenantId);
        var existingCategory = await _categoryRepository.FirstOrDefaultAsync(slugSpec, cancellationToken);
        if (existingCategory != null)
        {
            return Result.Failure<ProductCategoryDto>(
                Error.Conflict($"A product category with slug '{command.Slug}' already exists.", "NOIR-PRODUCT-001"));
        }

        // Validate parent exists if specified
        string? parentName = null;
        if (command.ParentId.HasValue)
        {
            var parentSpec = new ProductCategoryByIdSpec(command.ParentId.Value);
            var parent = await _categoryRepository.FirstOrDefaultAsync(parentSpec, cancellationToken);
            if (parent is null)
            {
                return Result.Failure<ProductCategoryDto>(
                    Error.NotFound($"Parent category with ID '{command.ParentId}' not found.", "NOIR-PRODUCT-002"));
            }
            parentName = parent.Name;
        }

        // Create the category
        var category = ProductCategory.Create(
            command.Name,
            command.Slug,
            command.ParentId,
            tenantId);

        // Set additional properties
        category.UpdateDetails(
            command.Name,
            command.Slug,
            command.Description,
            command.ImageUrl);

        category.UpdateSeo(command.MetaTitle, command.MetaDescription);
        category.SetSortOrder(command.SortOrder);

        await _categoryRepository.AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(ProductMapper.ToDto(category, parentName));
    }
}
