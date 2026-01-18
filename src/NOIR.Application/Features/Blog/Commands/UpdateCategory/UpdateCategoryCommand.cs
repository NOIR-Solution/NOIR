using NOIR.Application.Features.Blog.DTOs;

namespace NOIR.Application.Features.Blog.Commands.UpdateCategory;

/// <summary>
/// Command to update an existing blog category.
/// </summary>
public sealed record UpdateCategoryCommand(
    Guid Id,
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

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => Id;
    public string? GetTargetDisplayName() => Name;
    public string? GetActionDescription() => $"Updated blog category '{Name}'";
}
