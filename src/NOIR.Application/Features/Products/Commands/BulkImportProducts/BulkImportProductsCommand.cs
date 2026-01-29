namespace NOIR.Application.Features.Products.Commands.BulkImportProducts;

/// <summary>
/// Command to bulk import products from parsed CSV data.
/// </summary>
public sealed record BulkImportProductsCommand(
    List<ImportProductDto> Products)
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }
}

/// <summary>
/// Single product data for import.
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
    int? Stock);

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
