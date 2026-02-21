
namespace NOIR.Application.Features.Blog.Commands.CreateCategory;

/// <summary>
/// Command to create a new blog category.
/// </summary>
[RequiresFeature(ModuleNames.Content.Blog)]
public sealed record CreateCategoryCommand(
    string Name,
    string Slug,
    string? Description,
    string? MetaTitle,
    string? MetaDescription,
    string? ImageUrl,
    int SortOrder,
    Guid? ParentId) : IAuditableCommand<PostCategoryDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => UserId;
    public string? GetTargetDisplayName() => Name;
    public string? GetActionDescription() => $"Created blog category '{Name}'";
}
