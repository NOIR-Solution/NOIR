namespace NOIR.Application.Features.Products.Commands.CreateProduct;

/// <summary>
/// Command to create a new product.
/// </summary>
public sealed record CreateProductCommand(
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
    List<CreateProductVariantDto>? Variants,
    List<CreateProductImageDto>? Images) : IAuditableCommand<ProductDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => UserId;
    public string? GetTargetDisplayName() => Name;
    public string? GetActionDescription() => $"Created product '{Name}'";
}
