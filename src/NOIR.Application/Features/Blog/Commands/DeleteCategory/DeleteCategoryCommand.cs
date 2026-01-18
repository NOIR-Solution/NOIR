namespace NOIR.Application.Features.Blog.Commands.DeleteCategory;

/// <summary>
/// Command to soft delete a blog category.
/// </summary>
public sealed record DeleteCategoryCommand(
    Guid Id,
    string? CategoryName = null) : IAuditableCommand
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Delete;
    public object? GetTargetId() => Id;
    public string? GetTargetDisplayName() => CategoryName ?? Id.ToString();
    public string? GetActionDescription() => $"Deleted blog category '{GetTargetDisplayName()}'";
}
