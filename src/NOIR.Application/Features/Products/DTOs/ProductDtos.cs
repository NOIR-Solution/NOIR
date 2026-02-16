namespace NOIR.Application.Features.Products.DTOs;

/// <summary>
/// Full product details for editing.
/// </summary>
public sealed record ProductDto(
    Guid Id,
    string Name,
    string Slug,
    string? ShortDescription,
    string? Description,
    string? DescriptionHtml,
    decimal BasePrice,
    string Currency,
    ProductStatus Status,
    Guid? CategoryId,
    string? CategoryName,
    string? CategorySlug,
    Guid? BrandId,
    string? BrandName,
    string? Brand,
    string? Sku,
    string? Barcode,
    bool TrackInventory,
    string? MetaTitle,
    string? MetaDescription,
    int SortOrder,
    int TotalStock,
    bool InStock,
    List<ProductVariantDto> Variants,
    List<ProductImageDto> Images,
    List<ProductOptionDto> Options,
    IReadOnlyCollection<ProductAttributes.DTOs.ProductAttributeAssignmentDto>? Attributes,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt);

/// <summary>
/// Simplified product for list views.
/// </summary>
public sealed record ProductListDto(
    Guid Id,
    string Name,
    string Slug,
    string? ShortDescription,
    decimal BasePrice,
    string Currency,
    ProductStatus Status,
    string? CategoryName,
    Guid? BrandId,
    string? BrandName,
    string? Brand,
    string? Sku,
    int TotalStock,
    bool InStock,
    string? PrimaryImageUrl,
    IReadOnlyCollection<ProductAttributeDisplayDto>? DisplayAttributes,
    DateTimeOffset CreatedAt);

/// <summary>
/// Simplified attribute display for product cards (only showInProductCard=true).
/// </summary>
public sealed record ProductAttributeDisplayDto(
    string Code,
    string Name,
    string Type,
    string? DisplayValue,
    string? ColorCode);

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
    int SortOrder,
    Guid? ImageId);

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
    string? ShortDescription,
    string? Description,
    string? DescriptionHtml,
    decimal BasePrice,
    string Currency,
    Guid? CategoryId,
    Guid? BrandId,
    string? Brand,
    string? Sku,
    string? Barcode,
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
    string? ShortDescription,
    string? Description,
    string? DescriptionHtml,
    decimal BasePrice,
    string Currency,
    Guid? CategoryId,
    Guid? BrandId,
    string? Brand,
    string? Sku,
    string? Barcode,
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

// ===== Variant Management Request DTOs =====

/// <summary>
/// Request to add a variant to a product.
/// </summary>
public sealed record AddProductVariantRequest(
    string Name,
    decimal Price,
    string? Sku,
    decimal? CompareAtPrice,
    int StockQuantity,
    Dictionary<string, string>? Options,
    int SortOrder);

/// <summary>
/// Request to update a product variant.
/// </summary>
public sealed record UpdateProductVariantRequest(
    string Name,
    decimal Price,
    string? Sku,
    decimal? CompareAtPrice,
    int StockQuantity,
    Dictionary<string, string>? Options,
    int SortOrder);

// ===== Image Management Request DTOs =====

/// <summary>
/// Request to add an image to a product.
/// </summary>
public sealed record AddProductImageRequest(
    string Url,
    string? AltText,
    int SortOrder,
    bool IsPrimary);

/// <summary>
/// Request to update a product image.
/// </summary>
public sealed record UpdateProductImageRequest(
    string Url,
    string? AltText,
    int SortOrder);

// ===== Upload Result DTOs =====

/// <summary>
/// Result of uploading a product image.
/// Includes the created image info plus processing metadata.
/// </summary>
public sealed record ProductImageUploadResultDto(
    Guid Id,
    string Url,
    string? AltText,
    int SortOrder,
    bool IsPrimary,
    string? ThumbUrl,
    string? MediumUrl,
    string? LargeUrl,
    int? Width,
    int? Height,
    string? ThumbHash,
    string? DominantColor,
    string Message);

/// <summary>
/// Request to reorder product images in bulk.
/// </summary>
public sealed record ReorderProductImagesRequest(
    List<ImageSortOrderItem> Items);

/// <summary>
/// Single item for image reordering.
/// </summary>
public sealed record ImageSortOrderItem(
    Guid ImageId,
    int SortOrder);

/// <summary>
/// Request to reorder product categories in bulk.
/// </summary>
public sealed record ReorderProductCategoriesRequest(
    List<ReorderCategorySortOrderItem> Items);

/// <summary>
/// Single item for category reordering.
/// </summary>
public sealed record ReorderCategorySortOrderItem(
    Guid CategoryId,
    Guid? ParentId,
    int SortOrder);

// ===== Option DTOs =====

/// <summary>
/// Product option details (e.g., "Color", "Size").
/// </summary>
public sealed record ProductOptionDto(
    Guid Id,
    string Name,
    string? DisplayName,
    int SortOrder,
    List<ProductOptionValueDto> Values);

/// <summary>
/// Product option value details (e.g., "Red", "Large").
/// </summary>
public sealed record ProductOptionValueDto(
    Guid Id,
    string Value,
    string? DisplayValue,
    string? ColorCode,
    string? SwatchUrl,
    int SortOrder);

// ===== Option Management Request DTOs =====

/// <summary>
/// Request to add an option to a product.
/// </summary>
public sealed record AddProductOptionRequest(
    string Name,
    string? DisplayName,
    int SortOrder,
    List<AddProductOptionValueRequest>? Values);

/// <summary>
/// Request to update a product option.
/// </summary>
public sealed record UpdateProductOptionRequest(
    string Name,
    string? DisplayName,
    int SortOrder);

/// <summary>
/// Request to add a value to a product option.
/// </summary>
public sealed record AddProductOptionValueRequest(
    string Value,
    string? DisplayValue,
    string? ColorCode,
    string? SwatchUrl,
    int SortOrder);

/// <summary>
/// Request to update an option value.
/// </summary>
public sealed record UpdateProductOptionValueRequest(
    string Value,
    string? DisplayValue,
    string? ColorCode,
    string? SwatchUrl,
    int SortOrder);

// ===== Stats DTOs =====

/// <summary>
/// Product statistics for dashboard display.
/// Provides accurate global counts independent of current filters/pagination.
/// </summary>
public sealed record ProductStatsDto(
    int Total,
    int Active,
    int Draft,
    int Archived,
    int OutOfStock,
    int LowStock);

// ===== Bulk Operation DTOs =====

/// <summary>
/// Result of a bulk operation (publish, archive, delete).
/// </summary>
public sealed record BulkOperationResultDto(
    int Success,
    int Failed,
    List<BulkOperationErrorDto> Errors);

/// <summary>
/// Error details for a failed bulk operation item.
/// </summary>
public sealed record BulkOperationErrorDto(
    Guid ProductId,
    string? ProductName,
    string Message);
