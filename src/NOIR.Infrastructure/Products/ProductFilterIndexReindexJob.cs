namespace NOIR.Infrastructure.Products;

/// <summary>
/// Hangfire job for full reindexing of ProductFilterIndex.
/// Rebuilds the entire index from scratch - used for:
/// - Initial population
/// - Recovery from data corruption
/// - Schema changes requiring reindex
/// </summary>
public class ProductFilterIndexReindexJob : IScopedService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly AttributeJsonBuilder _attributeJsonBuilder;
    private readonly ILogger<ProductFilterIndexReindexJob> _logger;
    private readonly IDateTime _dateTime;

    private const int BatchSize = 100;

    public ProductFilterIndexReindexJob(
        ApplicationDbContext dbContext,
        AttributeJsonBuilder attributeJsonBuilder,
        ILogger<ProductFilterIndexReindexJob> logger,
        IDateTime dateTime)
    {
        _dbContext = dbContext;
        _attributeJsonBuilder = attributeJsonBuilder;
        _logger = logger;
        _dateTime = dateTime;
    }

    /// <summary>
    /// Main job entry point. Rebuilds the entire filter index.
    /// </summary>
    public async Task ExecuteAsync(string? tenantId = null, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting ProductFilterIndex reindex job for tenant: {TenantId}",
            tenantId ?? "ALL");
        var sw = Stopwatch.StartNew();

        try
        {
            var totalProcessed = 0;
            var totalCreated = 0;
            var totalUpdated = 0;

            // Get total count for progress reporting
            var totalProducts = await GetProductCountAsync(tenantId, ct);
            _logger.LogInformation("Found {Total} products to index", totalProducts);

            // Process in batches
            var lastProcessedId = Guid.Empty;
            while (true)
            {
                var products = await LoadProductBatchAsync(tenantId, lastProcessedId, ct);
                if (products.Count == 0) break;

                var (created, updated) = await ProcessProductBatchAsync(products, ct);
                totalCreated += created;
                totalUpdated += updated;
                totalProcessed += products.Count;

                lastProcessedId = products.Last().Id;

                _logger.LogInformation(
                    "Processed {Current}/{Total} products ({Percent:F1}%)",
                    totalProcessed, totalProducts,
                    (double)totalProcessed / totalProducts * 100);
            }

            sw.Stop();
            _logger.LogInformation(
                "ProductFilterIndex reindex completed in {ElapsedMs}ms. " +
                "Processed: {Processed}, Created: {Created}, Updated: {Updated}",
                sw.ElapsedMilliseconds, totalProcessed, totalCreated, totalUpdated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ProductFilterIndex reindex job failed");
            throw;
        }
    }

    /// <summary>
    /// Reindexes a single product. Used for targeted reindex after sync failures.
    /// </summary>
    public async Task ReindexProductAsync(Guid productId, CancellationToken ct = default)
    {
        _logger.LogDebug("Reindexing single product {ProductId}", productId);

        var product = await _dbContext.Products
            .TagWith("ProductFilterIndexReindexJob.ReindexProduct")
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == productId, ct);

        if (product == null)
        {
            _logger.LogWarning("Product {ProductId} not found for reindex", productId);
            return;
        }

        await ProcessSingleProductAsync(product, ct);
        await _dbContext.SaveChangesAsync(ct);
    }

    private async Task<int> GetProductCountAsync(string? tenantId, CancellationToken ct)
    {
        var query = _dbContext.Products
            .TagWith("ProductFilterIndexReindexJob.GetCount")
            .AsNoTracking();

        if (!string.IsNullOrEmpty(tenantId))
            query = query.Where(p => p.TenantId == tenantId);

        return await query.CountAsync(ct);
    }

    private async Task<List<Product>> LoadProductBatchAsync(
        string? tenantId,
        Guid lastProcessedId,
        CancellationToken ct)
    {
        var query = _dbContext.Products
            .TagWith("ProductFilterIndexReindexJob.LoadBatch")
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .Where(p => p.Id.CompareTo(lastProcessedId) > 0)
            .OrderBy(p => p.Id)
            .Take(BatchSize);

        if (!string.IsNullOrEmpty(tenantId))
            query = query.Where(p => p.TenantId == tenantId);

        return await query.ToListAsync(ct);
    }

    private async Task<(int created, int updated)> ProcessProductBatchAsync(
        List<Product> products,
        CancellationToken ct)
    {
        var created = 0;
        var updated = 0;

        // Load existing indexes for this batch
        var productIds = products.Select(p => p.Id).ToList();
        var existingIndexes = await _dbContext.ProductFilterIndexes
            .TagWith("ProductFilterIndexReindexJob.LoadExistingIndexes")
            .Where(fi => productIds.Contains(fi.ProductId))
            .ToDictionaryAsync(fi => fi.ProductId, ct);

        // Load categories and brands in bulk
        var categoryIds = products.Where(p => p.CategoryId.HasValue).Select(p => p.CategoryId!.Value).Distinct().ToList();
        var brandIds = products.Where(p => p.BrandId.HasValue).Select(p => p.BrandId!.Value).Distinct().ToList();

        var categories = await _dbContext.ProductCategories
            .TagWith("ProductFilterIndexReindexJob.LoadCategories")
            .Where(c => categoryIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, ct);

        var brands = await _dbContext.Brands
            .TagWith("ProductFilterIndexReindexJob.LoadBrands")
            .Where(b => brandIds.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, ct);

        // Load category paths in bulk
        var categoryPaths = await BuildCategoryPathsAsync(categories.Values.ToList(), ct);

        // Load attribute assignments in bulk
        var attributeAssignments = await _dbContext.ProductAttributeAssignments
            .TagWith("ProductFilterIndexReindexJob.LoadAssignments")
            .Include(pa => pa.Attribute)
            .Where(pa => productIds.Contains(pa.ProductId) && pa.VariantId == null)
            .GroupBy(pa => pa.ProductId)
            .ToDictionaryAsync(g => g.Key, g => g.ToList(), ct);

        var errors = new List<(Guid ProductId, Exception Error)>();

        foreach (var product in products)
        {
            try
            {
                var category = product.CategoryId.HasValue && categories.TryGetValue(product.CategoryId.Value, out var c) ? c : null;
                var brand = product.BrandId.HasValue && brands.TryGetValue(product.BrandId.Value, out var b) ? b : null;
                var categoryPath = category != null && categoryPaths.TryGetValue(category.Id, out var path) ? path : null;
                var assignments = attributeAssignments.TryGetValue(product.Id, out var a) ? a : new List<ProductAttributeAssignment>();
                var attributesJson = _attributeJsonBuilder.BuildAttributesJson(assignments);

                if (existingIndexes.TryGetValue(product.Id, out var filterIndex))
                {
                    filterIndex.UpdateFromProduct(product, category, brand, categoryPath);
                    filterIndex.SetAttributesJson(attributesJson);
                    updated++;
                }
                else
                {
                    filterIndex = ProductFilterIndex.Create(
                        product.Id,
                        product.Name,
                        product.Slug,
                        product.Status,
                        product.BasePrice,
                        product.Currency,
                        product.TenantId);

                    filterIndex.UpdateFromProduct(product, category, brand, categoryPath);
                    filterIndex.SetAttributesJson(attributesJson);
                    _dbContext.ProductFilterIndexes.Add(filterIndex);
                    created++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to index product {ProductId}", product.Id);
                errors.Add((product.Id, ex));
            }
        }

        await _dbContext.SaveChangesAsync(ct);

        if (errors.Count > 0)
        {
            _logger.LogWarning("Batch completed with {ErrorCount} errors out of {Total} products",
                errors.Count, products.Count);
        }

        return (created, updated);
    }

    private async Task ProcessSingleProductAsync(Product product, CancellationToken ct)
    {
        var category = product.CategoryId.HasValue
            ? await _dbContext.ProductCategories.FindAsync(new object[] { product.CategoryId.Value }, ct)
            : null;

        var brand = product.BrandId.HasValue
            ? await _dbContext.Brands.FindAsync(new object[] { product.BrandId.Value }, ct)
            : null;

        var categoryPath = category != null ? await BuildSingleCategoryPathAsync(category, ct) : null;

        var assignments = await _dbContext.ProductAttributeAssignments
            .TagWith("ProductFilterIndexReindexJob.LoadSingleAssignments")
            .Include(pa => pa.Attribute)
            .Where(pa => pa.ProductId == product.Id && pa.VariantId == null)
            .ToListAsync(ct);

        var attributesJson = _attributeJsonBuilder.BuildAttributesJson(assignments);

        var filterIndex = await _dbContext.ProductFilterIndexes
            .FirstOrDefaultAsync(fi => fi.ProductId == product.Id, ct);

        if (filterIndex != null)
        {
            filterIndex.UpdateFromProduct(product, category, brand, categoryPath);
            filterIndex.SetAttributesJson(attributesJson);
        }
        else
        {
            filterIndex = ProductFilterIndex.Create(
                product.Id,
                product.Name,
                product.Slug,
                product.Status,
                product.BasePrice,
                product.Currency,
                product.TenantId);

            filterIndex.UpdateFromProduct(product, category, brand, categoryPath);
            filterIndex.SetAttributesJson(attributesJson);
            _dbContext.ProductFilterIndexes.Add(filterIndex);
        }
    }

    private async Task<Dictionary<Guid, string>> BuildCategoryPathsAsync(
        List<ProductCategory> categories,
        CancellationToken ct)
    {
        var paths = new Dictionary<Guid, string>();
        var allCategoryIds = new HashSet<Guid>(categories.Select(c => c.Id));

        // Load all parent categories we might need
        foreach (var category in categories.Where(c => c.ParentId.HasValue))
        {
            var current = category;
            while (current.ParentId.HasValue && !allCategoryIds.Contains(current.ParentId.Value))
            {
                var parent = await _dbContext.ProductCategories.FindAsync(new object[] { current.ParentId.Value }, ct);
                if (parent == null) break;

                allCategoryIds.Add(parent.Id);
                current = parent;
            }
        }

        // Now build paths
        foreach (var category in categories)
        {
            paths[category.Id] = await BuildSingleCategoryPathAsync(category, ct);
        }

        return paths;
    }

    private async Task<string> BuildSingleCategoryPathAsync(ProductCategory category, CancellationToken ct)
    {
        var pathParts = new List<string> { category.Id.ToString() };
        var currentCategory = category;

        while (currentCategory.ParentId.HasValue)
        {
            var parent = await _dbContext.ProductCategories.FindAsync(new object[] { currentCategory.ParentId.Value }, ct);
            if (parent == null) break;

            pathParts.Insert(0, parent.Id.ToString());
            currentCategory = parent;
        }

        return string.Join("/", pathParts);
    }

}
