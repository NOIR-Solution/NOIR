namespace NOIR.Application.Features.Products.Specifications;

/// <summary>
/// Specification to retrieve products with filtering and pagination.
/// </summary>
public sealed class ProductsSpec : Specification<Product>
{
    public ProductsSpec(
        string? search = null,
        ProductStatus? status = null,
        Guid? categoryId = null,
        string? brand = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        bool? inStockOnly = null,
        bool? lowStockOnly = null,
        int lowStockThreshold = 10,
        int? skip = null,
        int? take = null,
        Dictionary<string, List<string>>? attributeFilters = null)
    {
        // Search filter
        Query.Where(p => string.IsNullOrEmpty(search) ||
                         p.Name.Contains(search) ||
                         (p.ShortDescription != null && p.ShortDescription.Contains(search)) ||
                         (p.Description != null && p.Description.Contains(search)) ||
                         (p.Sku != null && p.Sku.Contains(search)));

        // Status filter
        if (status.HasValue)
        {
            Query.Where(p => p.Status == status.Value);
        }

        // Category filter
        if (categoryId.HasValue)
        {
            Query.Where(p => p.CategoryId == categoryId.Value);
        }

        // Brand filter
        if (!string.IsNullOrEmpty(brand))
        {
            Query.Where(p => p.Brand == brand);
        }

        // Price range filter
        if (minPrice.HasValue)
        {
            Query.Where(p => p.BasePrice >= minPrice.Value);
        }
        if (maxPrice.HasValue)
        {
            Query.Where(p => p.BasePrice <= maxPrice.Value);
        }

        // In stock filter
        if (inStockOnly == true)
        {
            Query.Where(p => p.Variants.Any(v => v.StockQuantity > 0));
        }

        // Low stock filter: products with stock > 0 but below threshold (combined for optimal SQL)
        if (lowStockOnly == true)
        {
            Query.Where(p =>
                p.Variants.Any(v => v.StockQuantity > 0) &&
                p.Variants.Sum(v => v.StockQuantity) < lowStockThreshold);
        }

        // Attribute filters: filter by attribute code and display values
        // Dictionary key is attribute code, value is list of display values to match
        if (attributeFilters != null && attributeFilters.Count > 0)
        {
            foreach (var filter in attributeFilters)
            {
                var attributeCode = filter.Key;
                var values = filter.Value;

                if (values.Count > 0)
                {
                    // Filter products that have an assignment for this attribute code
                    // with a DisplayValue matching any of the selected values
                    Query.Where(p => p.AttributeAssignments.Any(
                        a => a.Attribute.Code == attributeCode &&
                             a.DisplayValue != null &&
                             values.Contains(a.DisplayValue)));
                }
            }
        }

        // Use split query to prevent Cartesian explosion with multiple collections
        Query.AsSplitQuery();

        // Include category for display
        Query.Include(p => p.Category!);

        // Include brand for display
        Query.Include(p => p.BrandEntity!);

        // Include images for list display
        Query.Include(p => p.Images);

        // Include attribute assignments with their attributes for display
        // Only attributes with ShowInProductCard=true will be mapped to DisplayAttributes
        Query.Include("AttributeAssignments.Attribute");

        // Ordering
        Query.OrderByDescending(p => p.CreatedAt)
             .ThenBy(p => p.Name);

        // Pagination
        if (skip.HasValue)
        {
            Query.Skip(skip.Value);
        }
        if (take.HasValue)
        {
            Query.Take(take.Value);
        }

        Query.TagWith("GetProducts");
    }
}

/// <summary>
/// Specification to count products matching filters.
/// </summary>
public sealed class ProductsCountSpec : Specification<Product>
{
    public ProductsCountSpec(
        string? search = null,
        ProductStatus? status = null,
        Guid? categoryId = null,
        string? brand = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        bool? inStockOnly = null,
        bool? lowStockOnly = null,
        int lowStockThreshold = 10,
        Dictionary<string, List<string>>? attributeFilters = null)
    {
        // Search filter
        Query.Where(p => string.IsNullOrEmpty(search) ||
                         p.Name.Contains(search) ||
                         (p.ShortDescription != null && p.ShortDescription.Contains(search)) ||
                         (p.Description != null && p.Description.Contains(search)) ||
                         (p.Sku != null && p.Sku.Contains(search)));

        // Status filter
        if (status.HasValue)
        {
            Query.Where(p => p.Status == status.Value);
        }

        // Category filter
        if (categoryId.HasValue)
        {
            Query.Where(p => p.CategoryId == categoryId.Value);
        }

        // Brand filter
        if (!string.IsNullOrEmpty(brand))
        {
            Query.Where(p => p.Brand == brand);
        }

        // Price range filter
        if (minPrice.HasValue)
        {
            Query.Where(p => p.BasePrice >= minPrice.Value);
        }
        if (maxPrice.HasValue)
        {
            Query.Where(p => p.BasePrice <= maxPrice.Value);
        }

        // In stock filter
        if (inStockOnly == true)
        {
            Query.Where(p => p.Variants.Any(v => v.StockQuantity > 0));
        }

        // Low stock filter: products with stock > 0 but below threshold (combined for optimal SQL)
        if (lowStockOnly == true)
        {
            Query.Where(p =>
                p.Variants.Any(v => v.StockQuantity > 0) &&
                p.Variants.Sum(v => v.StockQuantity) < lowStockThreshold);
        }

        // Attribute filters: filter by attribute code and display values
        if (attributeFilters != null && attributeFilters.Count > 0)
        {
            foreach (var filter in attributeFilters)
            {
                var attributeCode = filter.Key;
                var values = filter.Value;

                if (values.Count > 0)
                {
                    Query.Where(p => p.AttributeAssignments.Any(
                        a => a.Attribute.Code == attributeCode &&
                             a.DisplayValue != null &&
                             values.Contains(a.DisplayValue)));
                }
            }
        }

        Query.TagWith("CountProducts");
    }
}

/// <summary>
/// Specification to find a product by ID with all related data.
/// </summary>
public sealed class ProductByIdSpec : Specification<Product>
{
    public ProductByIdSpec(Guid id)
    {
        Query.Where(p => p.Id == id)
             .AsSplitQuery() // Prevent Cartesian explosion with multiple collections
             .Include(p => p.Category!)
             .Include(p => p.BrandEntity!)
             .Include(p => p.Variants)
             .Include(p => p.Images)
             .Include("Options.Values")
             .Include("AttributeAssignments.Attribute")
             .TagWith("GetProductById");
    }
}

/// <summary>
/// Specification to find a product by ID for update (with tracking).
/// </summary>
public sealed class ProductByIdForUpdateSpec : Specification<Product>
{
    public ProductByIdForUpdateSpec(Guid id)
    {
        Query.Where(p => p.Id == id)
             .AsSplitQuery() // Prevent Cartesian explosion with multiple collections
             .Include(p => p.Category!)
             .Include(p => p.BrandEntity!)
             .Include(p => p.Variants)
             .Include(p => p.Images)
             .Include("Options.Values")
             .AsTracking()
             .TagWith("GetProductByIdForUpdate");
    }
}

/// <summary>
/// Specification to find a product by ID for option updates (with tracking).
/// Only loads options and values, not variants or images for better performance.
/// </summary>
public sealed class ProductByIdForOptionUpdateSpec : Specification<Product>
{
    public ProductByIdForOptionUpdateSpec(Guid id)
    {
        Query.Where(p => p.Id == id)
             .Include("Options.Values")
             .AsTracking()
             .TagWith("GetProductByIdForOptionUpdate");
    }
}

/// <summary>
/// Specification to find a product by slug.
/// </summary>
public sealed class ProductBySlugSpec : Specification<Product>
{
    public ProductBySlugSpec(string slug, string? tenantId = null)
    {
        Query.Where(p => p.Slug == slug.ToLowerInvariant())
             .Where(p => tenantId == null || p.TenantId == tenantId)
             .AsSplitQuery() // Prevent Cartesian explosion with multiple collections
             .Include(p => p.Category!)
             .Include(p => p.BrandEntity!)
             .Include(p => p.Variants)
             .Include(p => p.Images)
             .Include("Options.Values")
             .TagWith("GetProductBySlug");
    }
}

/// <summary>
/// Specification to check if a product slug is unique within a tenant.
/// </summary>
public sealed class ProductSlugExistsSpec : Specification<Product>
{
    public ProductSlugExistsSpec(string slug, string? tenantId = null, Guid? excludeId = null)
    {
        Query.Where(p => p.Slug == slug.ToLowerInvariant())
             .Where(p => tenantId == null || p.TenantId == tenantId)
             .Where(p => excludeId == null || p.Id != excludeId)
             .TagWith("CheckProductSlugExists");
    }
}

/// <summary>
/// Specification to check if a product SKU is unique within a tenant.
/// </summary>
public sealed class ProductSkuExistsSpec : Specification<Product>
{
    public ProductSkuExistsSpec(string sku, string? tenantId = null, Guid? excludeId = null)
    {
        Query.Where(p => p.Sku == sku)
             .Where(p => tenantId == null || p.TenantId == tenantId)
             .Where(p => excludeId == null || p.Id != excludeId)
             .TagWith("CheckProductSkuExists");
    }
}

/// <summary>
/// Specification to get active (published) products for storefront.
/// </summary>
public sealed class ActiveProductsSpec : Specification<Product>
{
    public ActiveProductsSpec(Guid? categoryId = null, int? take = null)
    {
        Query.Where(p => p.Status == ProductStatus.Active);

        if (categoryId.HasValue)
        {
            Query.Where(p => p.CategoryId == categoryId.Value);
        }

        Query.Include(p => p.Images)
             .OrderByDescending(p => p.CreatedAt);

        if (take.HasValue)
        {
            Query.Take(take.Value);
        }

        Query.TagWith("GetActiveProducts");
    }
}

/// <summary>
/// Specification to get products by status.
/// </summary>
public sealed class ProductsByStatusSpec : Specification<Product>
{
    public ProductsByStatusSpec(ProductStatus status)
    {
        Query.Where(p => p.Status == status)
             .Include(p => p.Category!)
             .Include(p => p.Images)
             .OrderByDescending(p => p.CreatedAt)
             .TagWith("GetProductsByStatus");
    }
}

/// <summary>
/// Specification to get product with a specific variant by ID.
/// </summary>
public sealed class ProductWithVariantByIdSpec : Specification<Product>
{
    public ProductWithVariantByIdSpec(Guid productId, Guid variantId)
    {
        Query.Where(p => p.Id == productId)
            .Include(p => p.Variants.Where(v => v.Id == variantId))
            .Include(p => p.Images)
            .TagWith("ProductWithVariantById");
    }
}

/// <summary>
/// Specification to count products by status.
/// Uses lightweight query for efficient counting.
/// </summary>
public sealed class ProductsByStatusCountSpec : Specification<Product>
{
    public ProductsByStatusCountSpec(ProductStatus status)
    {
        Query.Where(p => p.Status == status)
             .TagWith($"CountProductsByStatus_{status}");
    }
}

/// <summary>
/// Specification to count products with low stock.
/// Low stock = has stock > 0 but below threshold.
/// </summary>
public sealed class ProductsLowStockCountSpec : Specification<Product>
{
    public ProductsLowStockCountSpec(int threshold)
    {
        // Products with any variant having stock > 0 but total stock below threshold
        Query.Where(p => p.Variants.Any(v => v.StockQuantity > 0))
             .Where(p => p.Variants.Sum(v => v.StockQuantity) < threshold)
             .TagWith("CountProductsLowStock");
    }
}

/// <summary>
/// Specification to get a product by ID with all collections loaded.
/// Used for operations that need full product data (duplicate, export).
/// </summary>
public sealed class ProductByIdWithCollectionsSpec : Specification<Product>
{
    public ProductByIdWithCollectionsSpec(Guid id)
    {
        Query.Where(p => p.Id == id)
             .AsSplitQuery() // Prevent Cartesian explosion with multiple collections
             .Include(p => p.Variants)
             .Include(p => p.Images)
             .Include("Options.Values")
             .TagWith("GetProductByIdWithCollections");
    }
}

/// <summary>
/// Specification to get multiple products by IDs with tracking for updates.
/// Used for bulk operations (publish, archive, delete).
/// </summary>
public sealed class ProductsByIdsForUpdateSpec : Specification<Product>
{
    public ProductsByIdsForUpdateSpec(List<Guid> ids)
    {
        Query.Where(p => ids.Contains(p.Id))
             .AsTracking()
             .TagWith("GetProductsByIdsForUpdate");
    }
}

/// <summary>
/// Specification to get all product categories for import lookup.
/// </summary>
public sealed class AllProductCategoriesSpec : Specification<ProductCategory>
{
    public AllProductCategoriesSpec()
    {
        Query.TagWith("GetAllProductCategories");
    }
}

/// <summary>
/// Specification to get products by multiple slugs for bulk lookup.
/// Used for import operations to check existing slugs efficiently.
/// </summary>
public sealed class ProductsBySlugsSpec : Specification<Product>
{
    public ProductsBySlugsSpec(IEnumerable<string> slugs)
    {
        var slugList = slugs.Select(s => s.ToLowerInvariant()).ToList();
        Query.Where(p => slugList.Contains(p.Slug))
             .TagWith("GetProductsBySlugs");
    }
}

/// <summary>
/// Specification to find a product by option ID.
/// Used for before-state resolution in audit logging.
/// </summary>
public sealed class ProductByOptionIdSpec : Specification<Product>
{
    public ProductByOptionIdSpec(Guid optionId)
    {
        Query.Where(p => p.Options.Any(o => o.Id == optionId))
             .Include("Options.Values")
             .TagWith("GetProductByOptionId");
    }
}

/// <summary>
/// Specification to find a product by option value ID.
/// Used for before-state resolution in audit logging.
/// </summary>
public sealed class ProductByOptionValueIdSpec : Specification<Product>
{
    public ProductByOptionValueIdSpec(Guid valueId)
    {
        Query.Where(p => p.Options.Any(o => o.Values.Any(v => v.Id == valueId)))
             .Include("Options.Values")
             .TagWith("GetProductByOptionValueId");
    }
}

/// <summary>
/// Specification to get products with all related data for export.
/// </summary>
public sealed class ProductsForExportSpec : Specification<Product>
{
    public ProductsForExportSpec(
        string? status,
        string? categoryId,
        bool includeAttributes,
        bool includeImages)
    {
        // Use split query to prevent Cartesian explosion with multiple collections
        Query.AsSplitQuery();

        // Include variants
        Query.Include(p => p.Variants);

        if (includeImages)
        {
            Query.Include(p => p.Images);
        }

        if (includeAttributes)
        {
            // Include attribute assignments (we'll load attributes separately for the lookup)
            Query.Include(p => p.AttributeAssignments);
        }

        // Filter by status
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<ProductStatus>(status, true, out var productStatus))
        {
            Query.Where(p => p.Status == productStatus);
        }

        // Filter by category
        if (!string.IsNullOrEmpty(categoryId) && Guid.TryParse(categoryId, out var catId))
        {
            Query.Where(p => p.CategoryId == catId);
        }

        Query.OrderBy(p => p.Name)
             .TagWith("ExportProducts");
    }
}
