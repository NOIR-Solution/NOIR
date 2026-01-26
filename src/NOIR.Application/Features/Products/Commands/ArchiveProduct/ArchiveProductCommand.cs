namespace NOIR.Application.Features.Products.Commands.ArchiveProduct;

/// <summary>
/// Command to archive a product (hide from catalog).
/// </summary>
public sealed record ArchiveProductCommand(
    Guid Id,
    string? ProductName = null) : IAuditableCommand<ProductDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => Id;
    public string? GetTargetDisplayName() => ProductName ?? Id.ToString();
    public string? GetActionDescription() => $"Archived product '{GetTargetDisplayName()}'";
}
