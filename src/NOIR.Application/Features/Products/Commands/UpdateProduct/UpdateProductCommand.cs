namespace NOIR.Application.Features.Products.Commands.UpdateProduct;

/// <summary>
/// Command to update an existing product.
/// </summary>
public sealed record UpdateProductCommand(
    Guid Id,
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
    int SortOrder) : IAuditableCommand<ProductDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => Id;
    public string? GetTargetDisplayName() => Name;
    public string? GetActionDescription() => $"Updated product '{Name}'";
}
