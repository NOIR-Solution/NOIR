namespace NOIR.Application.Features.Products.Commands.ReorderProductCategories;

/// <summary>
/// Single item for category reordering.
/// </summary>
public sealed record CategorySortOrderItem(
    Guid CategoryId,
    Guid? ParentId,
    int SortOrder);

/// <summary>
/// Command to reorder product categories in bulk.
/// Updates the sort order and parent of multiple categories in a single request.
/// </summary>
public sealed record ReorderProductCategoriesCommand(
    List<CategorySortOrderItem> Items) : IAuditableCommand<List<ProductCategoryListDto>>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => UserId;
    public string? GetTargetDisplayName() => $"Reordered {Items.Count} categories";
    public string? GetActionDescription() => $"Reordered {Items.Count} product categories";
}
