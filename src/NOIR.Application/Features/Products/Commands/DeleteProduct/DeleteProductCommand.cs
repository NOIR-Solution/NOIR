namespace NOIR.Application.Features.Products.Commands.DeleteProduct;

/// <summary>
/// Command to soft-delete a product.
/// </summary>
public sealed record DeleteProductCommand(
    Guid Id,
    string? ProductName = null) : IAuditableCommand
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Delete;
    public object? GetTargetId() => Id;
    public string? GetTargetDisplayName() => ProductName ?? Id.ToString();
    public string? GetActionDescription() => $"Deleted product '{GetTargetDisplayName()}'";
}
