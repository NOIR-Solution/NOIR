namespace NOIR.Application.Features.Blog.Commands.ReorderCategories;

/// <summary>
/// Single item for blog category reordering.
/// </summary>
public sealed record BlogCategorySortOrderItem(
    Guid CategoryId,
    Guid? ParentId,
    int SortOrder);

/// <summary>
/// Command to reorder blog categories in bulk.
/// Updates the sort order and parent of multiple categories in a single request.
/// </summary>
public sealed record ReorderBlogCategoriesCommand(
    List<BlogCategorySortOrderItem> Items) : IAuditableCommand<List<PostCategoryListDto>>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => UserId;
    public string? GetTargetDisplayName() => $"Reordered {Items.Count} categories";
    public string? GetActionDescription() => $"Reordered {Items.Count} blog categories";
}
