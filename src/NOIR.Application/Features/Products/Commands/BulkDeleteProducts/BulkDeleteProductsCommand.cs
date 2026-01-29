namespace NOIR.Application.Features.Products.Commands.BulkDeleteProducts;

/// <summary>
/// Command to soft-delete multiple products in a single operation.
/// </summary>
public sealed record BulkDeleteProductsCommand(
    List<Guid> ProductIds) : IAuditableCommand
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Delete;
    public object? GetTargetId() => ProductIds.Count == 1 ? ProductIds[0] : string.Join(",", ProductIds.Take(5));
    public string? GetTargetDisplayName() => $"{ProductIds.Count} products";
    public string? GetActionDescription() => $"Bulk deleted {ProductIds.Count} products";
}
