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

        // Check for child categories (efficient query - doesn't load all categories)
        var childrenSpec = new ProductCategoryHasChildrenSpec(command.Id);
        var hasChildren = await _categoryRepository.AnyAsync(childrenSpec, cancellationToken);
        if (hasChildren)
        {
            return Result.Failure<bool>(
                Error.Conflict("Cannot delete a category that has child categories. Please delete or reassign child categories first.", "NOIR-PRODUCT-005"));
        }

        // Check for products in this category (efficient query - doesn't load all products)
        var productsSpec = new ProductCategoryHasProductsSpec(command.Id);
        var hasProducts = await _productRepository.AnyAsync(productsSpec, cancellationToken);
        if (hasProducts)
        {
            return Result.Failure<bool>(
                Error.Conflict("Cannot delete a category that has products. Please reassign products to another category first.", "NOIR-PRODUCT-006"));
        }

        // Raise domain event and soft delete the category (handled by interceptor)
        category.MarkAsDeleted();
        _categoryRepository.Remove(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
