namespace NOIR.Application.Features.Products.Commands.BulkPublishProducts;

/// <summary>
/// Command to publish multiple products in a single operation.
/// </summary>
public sealed record BulkPublishProductsCommand(
    List<Guid> ProductIds) : IAuditableCommand
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => ProductIds.Count == 1 ? ProductIds[0] : string.Join(",", ProductIds.Take(5));
    public string? GetTargetDisplayName() => $"{ProductIds.Count} products";
    public string? GetActionDescription() => $"Bulk published {ProductIds.Count} products";
}
