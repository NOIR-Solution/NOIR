namespace NOIR.Application.Features.Products.Queries.ExportProducts;

/// <summary>
/// Query to export products as flat rows for CSV export.
/// Each variant becomes a separate row.
/// </summary>
public sealed record ExportProductsQuery(
    string? CategoryId = null,
    string? Status = null,
    bool IncludeAttributes = true,
    bool IncludeImages = true);

/// <summary>
/// Single export row representing a product or variant.
/// Flat format suitable for CSV export.
/// </summary>
public sealed record ExportProductRowDto(
    string Name,
    string Slug,
    string? Sku,
    string? Barcode,
    decimal BasePrice,
    string Currency,
    string Status,
    string? CategoryName,
    string? Brand,
    string? ShortDescription,
    // Variant info (null for products without variants)
    string? VariantName,
    decimal? VariantPrice,
    decimal? CompareAtPrice,
    int Stock,
    // Images as pipe-separated URLs
    string? Images,
    // Dynamic attributes as key-value pairs (attr_code -> value)
    Dictionary<string, string> Attributes);

/// <summary>
/// Result of export operation with rows and attribute column headers.
/// </summary>
public sealed record ExportProductsResultDto(
    List<ExportProductRowDto> Rows,
    List<string> AttributeColumns);
