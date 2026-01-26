namespace NOIR.Application.Features.Products.Commands.DeleteProductCategory;

/// <summary>
/// Wolverine handler for soft deleting a product category.
/// </summary>
public class DeleteProductCategoryCommandHandler
{
    private readonly IRepository<ProductCategory, Guid> _categoryRepository;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProductCategoryCommandHandler(
        IRepository<ProductCategory, Guid> categoryRepository,
        IRepository<Product, Guid> productRepository,
        IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(
        DeleteProductCategoryCommand command,
        CancellationToken cancellationToken)
    {
        // Get category with tracking
        var categorySpec = new ProductCategoryByIdForUpdateSpec(command.Id);
        var category = await _categoryRepository.FirstOrDefaultAsync(categorySpec, cancellationToken);

        if (category is null)
        {
            return Result.Failure<bool>(
                Error.NotFound($"Product category with ID '{command.Id}' not found.", "NOIR-PRODUCT-003"));
        }

        // Check for child categories
        var childrenSpec = new ProductCategoriesSpec();
        var categories = await _categoryRepository.ListAsync(childrenSpec, cancellationToken);
        var hasChildren = categories.Any(c => c.ParentId == command.Id);
        if (hasChildren)
        {
            return Result.Failure<bool>(
                Error.Conflict("Cannot delete a category that has child categories. Please delete or reassign child categories first.", "NOIR-PRODUCT-005"));
        }

        // Check for products in this category
        var productsSpec = new ProductCategoryHasProductsSpec(command.Id);
        var productsInCategory = await _productRepository.ListAsync(productsSpec, cancellationToken);
        if (productsInCategory.Any())
        {
            return Result.Failure<bool>(
                Error.Conflict("Cannot delete a category that has products. Please reassign products to another category first.", "NOIR-PRODUCT-006"));
        }

        // Soft delete the category (handled by interceptor)
        _categoryRepository.Remove(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
