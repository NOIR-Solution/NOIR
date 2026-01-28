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
        int? skip = null,
        int? take = null)
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

        // Include category for display
        Query.Include(p => p.Category!);

        // Include images for list display
        Query.Include(p => p.Images);

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
        decimal? maxPrice = null)
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
             .Include(p => p.Category!)
             .Include(p => p.Variants)
             .Include(p => p.Images)
             .Include("Options.Values")
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
             .Include(p => p.Category!)
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
             .Include(p => p.Category!)
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
