using NOIR.Application.Features.ProductFilterIndex.Services;
using NOIR.Domain.Events.Product;

namespace NOIR.Application.Features.ProductFilterIndex.EventHandlers;

/// <summary>
/// Handles domain events to keep ProductFilterIndex synchronized with product changes.
/// Implements incremental sync for high-performance filtering.
/// </summary>
public class ProductFilterIndexSyncHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<ProductCategory, Guid> _categoryRepository;
    private readonly IRepository<Brand, Guid> _brandRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AttributeJsonBuilder _attributeJsonBuilder;
    private readonly ILogger<ProductFilterIndexSyncHandler> _logger;

    public ProductFilterIndexSyncHandler(
        IApplicationDbContext dbContext,
        IRepository<Product, Guid> productRepository,
        IRepository<ProductCategory, Guid> categoryRepository,
        IRepository<Brand, Guid> brandRepository,
        IUnitOfWork unitOfWork,
        AttributeJsonBuilder attributeJsonBuilder,
        ILogger<ProductFilterIndexSyncHandler> logger)
    {
        _dbContext = dbContext;
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _brandRepository = brandRepository;
        _unitOfWork = unitOfWork;
        _attributeJsonBuilder = attributeJsonBuilder;
        _logger = logger;
    }

    /// <summary>
    /// Handles product creation by creating a new filter index entry.
    /// </summary>
    public async Task Handle(ProductCreatedEvent evt, CancellationToken ct)
    {
        _logger.LogDebug("Syncing filter index for new product {ProductId}", evt.ProductId);

        var product = await LoadProductWithRelationsAsync(evt.ProductId, ct);
        if (product == null)
        {
            _logger.LogWarning("Product {ProductId} not found for filter index creation", evt.ProductId);
            return;
        }

        var (category, categoryPath) = await LoadCategoryWithPathAsync(product.CategoryId, ct);
        var brand = product.BrandId.HasValue
            ? await _brandRepository.GetByIdAsync(product.BrandId.Value, ct)
            : null;

        var filterIndex = NOIR.Domain.Entities.Product.ProductFilterIndex.Create(
            product.Id,
            product.Name,
            product.Slug,
            product.Status,
            product.BasePrice,
            product.Currency,
            product.TenantId);

        filterIndex.UpdateFromProduct(product, category, brand, categoryPath);
        await UpdateAttributesJsonAsync(filterIndex, product.Id, ct);

        _dbContext.ProductFilterIndexes.Add(filterIndex);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogDebug("Created filter index for product {ProductId}", evt.ProductId);
    }

    /// <summary>
    /// Handles product updates by refreshing the filter index entry.
    /// </summary>
    public async Task Handle(ProductUpdatedEvent evt, CancellationToken ct)
    {
        _logger.LogDebug("Syncing filter index for updated product {ProductId}", evt.ProductId);
        await SyncProductFilterIndexAsync(evt.ProductId, ct);
    }

    /// <summary>
    /// Handles product publication by updating status in filter index.
    /// </summary>
    public async Task Handle(ProductPublishedEvent evt, CancellationToken ct)
    {
        _logger.LogDebug("Syncing filter index for published product {ProductId}", evt.ProductId);
        await SyncProductFilterIndexAsync(evt.ProductId, ct);
    }

    /// <summary>
    /// Handles product archival by updating status in filter index.
    /// </summary>
    public async Task Handle(ProductArchivedEvent evt, CancellationToken ct)
    {
        _logger.LogDebug("Syncing filter index for archived product {ProductId}", evt.ProductId);
        await SyncProductFilterIndexAsync(evt.ProductId, ct);
    }

    /// <summary>
    /// Handles stock changes by updating inventory info in filter index.
    /// </summary>
    public async Task Handle(ProductStockChangedEvent evt, CancellationToken ct)
    {
        _logger.LogDebug("Syncing filter index stock for product {ProductId}", evt.ProductId);

        var filterIndex = await _dbContext.ProductFilterIndexes
            .FirstOrDefaultAsync(fi => fi.ProductId == evt.ProductId, ct);

        if (filterIndex == null)
        {
            // Index doesn't exist yet - sync full product
            await SyncProductFilterIndexAsync(evt.ProductId, ct);
            return;
        }

        // Load product to get current total stock
        var product = await LoadProductWithRelationsAsync(evt.ProductId, ct);
        if (product == null) return;

        filterIndex.UpdateStock(product.TotalStock, product.InStock);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogDebug("Updated filter index stock for product {ProductId}", evt.ProductId);
    }

    /// <summary>
    /// Handles attribute assignment changes by updating attributes JSON.
    /// </summary>
    public async Task Handle(ProductAttributeAssignmentChangedEvent evt, CancellationToken ct)
    {
        _logger.LogDebug("Syncing filter index attributes for product {ProductId}", evt.ProductId);

        var filterIndex = await _dbContext.ProductFilterIndexes
            .FirstOrDefaultAsync(fi => fi.ProductId == evt.ProductId, ct);

        if (filterIndex == null)
        {
            // Index doesn't exist yet - sync full product
            await SyncProductFilterIndexAsync(evt.ProductId, ct);
            return;
        }

        await UpdateAttributesJsonAsync(filterIndex, evt.ProductId, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogDebug("Updated filter index attributes for product {ProductId}", evt.ProductId);
    }

    #region Private Methods

    private async Task SyncProductFilterIndexAsync(Guid productId, CancellationToken ct)
    {
        var product = await LoadProductWithRelationsAsync(productId, ct);
        if (product == null)
        {
            _logger.LogWarning("Product {ProductId} not found for filter index sync", productId);
            return;
        }

        var filterIndex = await _dbContext.ProductFilterIndexes
            .FirstOrDefaultAsync(fi => fi.ProductId == productId, ct);

        var (category, categoryPath) = await LoadCategoryWithPathAsync(product.CategoryId, ct);
        var brand = product.BrandId.HasValue
            ? await _brandRepository.GetByIdAsync(product.BrandId.Value, ct)
            : null;

        if (filterIndex == null)
        {
            // Create new index entry
            filterIndex = NOIR.Domain.Entities.Product.ProductFilterIndex.Create(
                product.Id,
                product.Name,
                product.Slug,
                product.Status,
                product.BasePrice,
                product.Currency,
                product.TenantId);

            filterIndex.UpdateFromProduct(product, category, brand, categoryPath);
            await UpdateAttributesJsonAsync(filterIndex, product.Id, ct);

            _dbContext.ProductFilterIndexes.Add(filterIndex);
        }
        else
        {
            // Update existing index entry
            filterIndex.UpdateFromProduct(product, category, brand, categoryPath);
            await UpdateAttributesJsonAsync(filterIndex, product.Id, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<Product?> LoadProductWithRelationsAsync(Guid productId, CancellationToken ct)
    {
        return await _productRepository.FirstOrDefaultAsync(
            new ProductByIdWithRelationsSpec(productId), ct);
    }

    private async Task<(ProductCategory? category, string? path)> LoadCategoryWithPathAsync(
        Guid? categoryId,
        CancellationToken ct)
    {
        if (!categoryId.HasValue)
            return (null, null);

        var category = await _categoryRepository.GetByIdAsync(categoryId.Value, ct);
        if (category == null)
            return (null, null);

        // Build materialized path for hierarchy queries
        var path = await BuildCategoryPathAsync(category, ct);
        return (category, path);
    }

    private async Task<string> BuildCategoryPathAsync(ProductCategory category, CancellationToken ct)
    {
        var pathParts = new List<string> { category.Id.ToString() };
        var currentCategory = category;

        // Walk up the hierarchy to build path
        while (currentCategory.ParentId.HasValue)
        {
            var parent = await _categoryRepository.GetByIdAsync(currentCategory.ParentId.Value, ct);
            if (parent == null) break;

            pathParts.Insert(0, parent.Id.ToString());
            currentCategory = parent;
        }

        return string.Join("/", pathParts);
    }

    private async Task UpdateAttributesJsonAsync(
        NOIR.Domain.Entities.Product.ProductFilterIndex filterIndex,
        Guid productId,
        CancellationToken ct)
    {
        // Load product attribute assignments
        var assignments = await _dbContext.ProductAttributeAssignments
            .Include(pa => pa.Attribute)
            .Where(pa => pa.ProductId == productId && pa.VariantId == null)
            .ToListAsync(ct);

        var json = _attributeJsonBuilder.BuildAttributesJson(assignments);
        filterIndex.SetAttributesJson(json);
    }

    #endregion
}

/// <summary>
/// Specification to load product with all relations needed for filter index.
/// </summary>
internal sealed class ProductByIdWithRelationsSpec : Specification<Product>
{
    public ProductByIdWithRelationsSpec(Guid productId)
    {
        Query
            .Where(p => p.Id == productId)
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .AsTracking()
            .TagWith("ProductByIdWithRelationsSpec");
    }
}
