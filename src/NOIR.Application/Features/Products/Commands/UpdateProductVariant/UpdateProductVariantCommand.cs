namespace NOIR.Application.Features.Products.Commands.UpdateProductVariant;

/// <summary>
/// Command to update a product variant.
/// </summary>
public sealed record UpdateProductVariantCommand(
    Guid ProductId,
    Guid VariantId,
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

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => VariantId;
    public string? GetTargetDisplayName() => Name;
    public string? GetActionDescription() => $"Updated variant '{Name}'";
}
