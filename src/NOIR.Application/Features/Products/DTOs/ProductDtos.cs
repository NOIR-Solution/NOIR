namespace NOIR.Application.Features.Products.DTOs;

/// <summary>
/// Full product details for editing.
/// </summary>
public sealed record ProductDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? DescriptionHtml,
    decimal BasePrice,
    string Currency,
    ProductStatus Status,
    Guid? CategoryId,
    string? CategoryName,
    string? CategorySlug,
    string? Brand,
    string? Sku,
    string? Barcode,
    decimal? Weight,
    bool TrackInventory,
    string? MetaTitle,
    string? MetaDescription,
    int SortOrder,
    int TotalStock,
    bool InStock,
    List<ProductVariantDto> Variants,
    List<ProductImageDto> Images,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt);

/// <summary>
/// Simplified product for list views.
/// </summary>
public sealed record ProductListDto(
    Guid Id,
    string Name,
    string Slug,
    decimal BasePrice,
    string Currency,
    ProductStatus Status,
    string? CategoryName,
    string? Brand,
    string? Sku,
    int TotalStock,
    bool InStock,
    string? PrimaryImageUrl,
    DateTimeOffset CreatedAt);

/// <summary>
/// Product variant details.
/// </summary>
public sealed record ProductVariantDto(
    Guid Id,
    string Name,
    string? Sku,
    decimal Price,
    decimal? CompareAtPrice,
    int StockQuantity,
    bool InStock,
    bool LowStock,
    bool OnSale,
    Dictionary<string, string>? Options,
    int SortOrder);

/// <summary>
/// Product image details.
/// </summary>
public sealed record ProductImageDto(
    Guid Id,
    string Url,
    string? AltText,
    int SortOrder,
    bool IsPrimary);

/// <summary>
/// Product category with hierarchy support.
/// </summary>
public sealed record ProductCategoryDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? MetaTitle,
    string? MetaDescription,
    string? ImageUrl,
    int SortOrder,
    int ProductCount,
    Guid? ParentId,
    string? ParentName,
    List<ProductCategoryDto>? Children,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt);

/// <summary>
/// Simplified category for list views and dropdowns.
/// </summary>
public sealed record ProductCategoryListDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    int SortOrder,
    int ProductCount,
    Guid? ParentId,
    string? ParentName,
    int ChildCount);

// ===== Request DTOs =====

/// <summary>
/// Request to create a new product category.
/// </summary>
public sealed record CreateProductCategoryRequest(
    string Name,
    string Slug,
    string? Description,
    string? MetaTitle,
    string? MetaDescription,
    string? ImageUrl,
    int SortOrder,
    Guid? ParentId);

/// <summary>
/// Request to update a product category.
/// </summary>
public sealed record UpdateProductCategoryRequest(
    string Name,
    string Slug,
    string? Description,
    string? MetaTitle,
    string? MetaDescription,
    string? ImageUrl,
    int SortOrder,
    Guid? ParentId);

/// <summary>
/// Request to create a new product.
/// </summary>
public sealed record CreateProductRequest(
    string Name,
    string Slug,
    string? Description,
    string? DescriptionHtml,
    decimal BasePrice,
    string Currency,
    Guid? CategoryId,
    string? Brand,
    string? Sku,
    string? Barcode,
    decimal? Weight,
    bool TrackInventory,
    string? MetaTitle,
    string? MetaDescription,
    int SortOrder,
    List<CreateProductVariantRequest>? Variants,
    List<CreateProductImageRequest>? Images);

/// <summary>
/// Request to update a product.
/// </summary>
public sealed record UpdateProductRequest(
    string Name,
    string Slug,
    string? Description,
    string? DescriptionHtml,
    decimal BasePrice,
    string Currency,
    Guid? CategoryId,
    string? Brand,
    string? Sku,
    string? Barcode,
    decimal? Weight,
    bool TrackInventory,
    string? MetaTitle,
    string? MetaDescription,
    int SortOrder);

/// <summary>
/// Request to create a product variant.
/// </summary>
public sealed record CreateProductVariantRequest(
    string Name,
    string? Sku,
    decimal Price,
    decimal? CompareAtPrice,
    int StockQuantity,
    Dictionary<string, string>? Options,
    int SortOrder);

/// <summary>
/// Request to create a product image.
/// </summary>
public sealed record CreateProductImageRequest(
    string Url,
    string? AltText,
    int SortOrder,
    bool IsPrimary);

// ===== Command DTOs =====

/// <summary>
/// DTO for creating a product variant (used in CreateProductCommand).
/// </summary>
public sealed record CreateProductVariantDto(
    string Name,
    string? Sku,
    decimal Price,
    decimal? CompareAtPrice,
    int StockQuantity,
    Dictionary<string, string>? Options,
    int SortOrder);

/// <summary>
/// DTO for creating a product image (used in CreateProductCommand).
/// </summary>
public sealed record CreateProductImageDto(
    string Url,
    string? AltText,
    int SortOrder,
    bool IsPrimary);
