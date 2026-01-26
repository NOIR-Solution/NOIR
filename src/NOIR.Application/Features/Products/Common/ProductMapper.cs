namespace NOIR.Application.Features.Products.Common;

/// <summary>
/// Centralized mapping utilities for Product-related entities to DTOs.
/// Eliminates duplication across command and query handlers.
/// </summary>
public static class ProductMapper
{
    /// <summary>
    /// Maps a Product entity to ProductDto with explicit category info.
    /// Use when category info is fetched separately (command handlers).
    /// </summary>
    public static ProductDto ToDto(
        Product product,
        string? categoryName,
        string? categorySlug,
        List<ProductVariantDto> variants,
        List<ProductImageDto> images)
    {
        return new ProductDto(
            product.Id,
            product.Name,
            product.Slug,
            product.Description,
            product.DescriptionHtml,
            product.BasePrice,
            product.Currency,
            product.Status,
            product.CategoryId,
            categoryName,
            categorySlug,
            product.Brand,
            product.Sku,
            product.Barcode,
            product.Weight,
            product.TrackInventory,
            product.MetaTitle,
            product.MetaDescription,
            product.SortOrder,
            product.TotalStock,
            product.InStock,
            variants,
            images,
            product.CreatedAt,
            product.ModifiedAt);
    }

    /// <summary>
    /// Maps a Product entity to ProductDto using navigation property for category.
    /// Use when category is eager-loaded (query handlers).
    /// </summary>
    public static ProductDto ToDto(Product product)
    {
        var variants = product.Variants
            .OrderBy(v => v.SortOrder)
            .Select(ToDto)
            .ToList();

        var images = product.Images
            .OrderBy(i => i.SortOrder)
            .Select(ToDto)
            .ToList();

        return new ProductDto(
            product.Id,
            product.Name,
            product.Slug,
            product.Description,
            product.DescriptionHtml,
            product.BasePrice,
            product.Currency,
            product.Status,
            product.CategoryId,
            product.Category?.Name,
            product.Category?.Slug,
            product.Brand,
            product.Sku,
            product.Barcode,
            product.Weight,
            product.TrackInventory,
            product.MetaTitle,
            product.MetaDescription,
            product.SortOrder,
            product.TotalStock,
            product.InStock,
            variants,
            images,
            product.CreatedAt,
            product.ModifiedAt);
    }

    /// <summary>
    /// Maps a Product entity to ProductDto, automatically mapping variants and images.
    /// Use when collections are already loaded.
    /// </summary>
    public static ProductDto ToDtoWithCollections(
        Product product,
        string? categoryName,
        string? categorySlug)
    {
        var variants = product.Variants
            .OrderBy(v => v.SortOrder)
            .Select(ToDto)
            .ToList();

        var images = product.Images
            .OrderBy(i => i.SortOrder)
            .Select(ToDto)
            .ToList();

        return ToDto(product, categoryName, categorySlug, variants, images);
    }

    /// <summary>
    /// Maps a ProductVariant entity to ProductVariantDto.
    /// </summary>
    public static ProductVariantDto ToDto(ProductVariant variant)
    {
        return new ProductVariantDto(
            variant.Id,
            variant.Name,
            variant.Sku,
            variant.Price,
            variant.CompareAtPrice,
            variant.StockQuantity,
            variant.InStock,
            variant.LowStock,
            variant.OnSale,
            variant.GetOptions(),
            variant.SortOrder);
    }

    /// <summary>
    /// Maps a ProductImage entity to ProductImageDto.
    /// </summary>
    public static ProductImageDto ToDto(ProductImage image)
    {
        return new ProductImageDto(
            image.Id,
            image.Url,
            image.AltText,
            image.SortOrder,
            image.IsPrimary);
    }

    /// <summary>
    /// Maps a Product entity to ProductListDto for list/grid views.
    /// Selects primary image or first available image.
    /// </summary>
    public static ProductListDto ToListDto(Product product)
    {
        var primaryImage = product.Images.FirstOrDefault(i => i.IsPrimary)
                        ?? product.Images.FirstOrDefault();

        return new ProductListDto(
            product.Id,
            product.Name,
            product.Slug,
            product.BasePrice,
            product.Currency,
            product.Status,
            product.Category?.Name,
            product.Brand,
            product.Sku,
            product.TotalStock,
            product.InStock,
            primaryImage?.Url,
            product.CreatedAt);
    }

    /// <summary>
    /// Maps a ProductCategory entity to ProductCategoryDto with explicit parent info.
    /// Use when parent info is fetched separately (command handlers).
    /// </summary>
    public static ProductCategoryDto ToDto(ProductCategory category, string? parentName)
    {
        return new ProductCategoryDto(
            category.Id,
            category.Name,
            category.Slug,
            category.Description,
            category.MetaTitle,
            category.MetaDescription,
            category.ImageUrl,
            category.SortOrder,
            category.ProductCount,
            category.ParentId,
            parentName,
            null, // Children not loaded in command context
            category.CreatedAt,
            category.ModifiedAt);
    }

    /// <summary>
    /// Maps a ProductCategory entity to ProductCategoryDto using navigation property.
    /// Use when parent is eager-loaded (query handlers).
    /// </summary>
    public static ProductCategoryDto ToDto(ProductCategory category)
    {
        return new ProductCategoryDto(
            category.Id,
            category.Name,
            category.Slug,
            category.Description,
            category.MetaTitle,
            category.MetaDescription,
            category.ImageUrl,
            category.SortOrder,
            category.ProductCount,
            category.ParentId,
            category.Parent?.Name,
            null, // Children mapped separately if needed
            category.CreatedAt,
            category.ModifiedAt);
    }

    /// <summary>
    /// Maps a ProductCategory entity to ProductCategoryDto with children.
    /// Use for hierarchical category queries.
    /// </summary>
    public static ProductCategoryDto ToDtoWithChildren(
        ProductCategory category,
        List<ProductCategoryDto>? children)
    {
        return new ProductCategoryDto(
            category.Id,
            category.Name,
            category.Slug,
            category.Description,
            category.MetaTitle,
            category.MetaDescription,
            category.ImageUrl,
            category.SortOrder,
            category.ProductCount,
            category.ParentId,
            category.Parent?.Name,
            children,
            category.CreatedAt,
            category.ModifiedAt);
    }

    /// <summary>
    /// Maps a ProductCategory entity to ProductCategoryListDto.
    /// </summary>
    public static ProductCategoryListDto ToListDto(ProductCategory category)
    {
        return new ProductCategoryListDto(
            category.Id,
            category.Name,
            category.Slug,
            category.Description,
            category.SortOrder,
            category.ProductCount,
            category.ParentId,
            category.Parent?.Name,
            category.Children?.Count ?? 0);
    }
}
