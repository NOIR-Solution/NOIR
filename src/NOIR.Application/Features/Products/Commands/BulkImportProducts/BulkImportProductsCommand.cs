namespace NOIR.Application.Features.Products.Commands.BulkImportProducts;

/// <summary>
/// Command to bulk import products from parsed CSV data.
/// Supports variants, images, and attributes in a flat CSV format.
/// </summary>
public sealed record BulkImportProductsCommand(
    List<ImportProductDto> Products)
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }
}

/// <summary>
/// Single product/variant row data for import.
/// Flat format where multiple rows with same ProductSlug create variants.
/// </summary>
public sealed record ImportProductDto(
    string Name,
    string? Slug,
    decimal BasePrice,
    string? Currency,
    string? ShortDescription,
    string? Sku,
    string? Barcode,
    string? CategoryName,
    string? Brand,
    int? Stock,
    // New fields for enhanced import
    string? VariantName,
    decimal? VariantPrice,
    decimal? CompareAtPrice,
    string? Images,              // Pipe-separated URLs: "url1|url2|url3"
    Dictionary<string, string>? Attributes);  // attr_code -> value

/// <summary>
/// Result of bulk import operation.
/// </summary>
public sealed record BulkImportResultDto(
    int Success,
    int Failed,
    List<ImportErrorDto> Errors);

/// <summary>
/// Error details for a failed import row.
/// </summary>
public sealed record ImportErrorDto(
    int Row,
    string Message);
