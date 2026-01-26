namespace NOIR.Application.Features.Products.Commands.DeleteProductCategory;

/// <summary>
/// Command to soft delete a product category.
/// </summary>
public sealed record DeleteProductCategoryCommand(
    Guid Id,
    string? CategoryName = null) : IAuditableCommand
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Delete;
    public object? GetTargetId() => Id;
    public string? GetTargetDisplayName() => CategoryName ?? Id.ToString();
    public string? GetActionDescription() => $"Deleted product category '{GetTargetDisplayName()}'";
}
