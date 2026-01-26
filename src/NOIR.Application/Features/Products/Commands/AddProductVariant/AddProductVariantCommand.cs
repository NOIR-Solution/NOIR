namespace NOIR.Application.Features.Products.Commands.AddProductVariant;

/// <summary>
/// Command to add a variant to a product.
/// </summary>
public sealed record AddProductVariantCommand(
    Guid ProductId,
    string Name,
    decimal Price,
    string? Sku,
    decimal? CompareAtPrice,
    int StockQuantity,
    Dictionary<string, string>? Options,
    int SortOrder) : IAuditableCommand<ProductVariantDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => ProductId;
    public string? GetTargetDisplayName() => Name;
    public string? GetActionDescription() => $"Added variant '{Name}' to product";
}
