using NOIR.Application.Features.Blog.DTOs;

namespace NOIR.Application.Features.Blog.Commands.UpdatePost;

/// <summary>
/// Command to update an existing blog post.
/// </summary>
public sealed record UpdatePostCommand(
    Guid Id,
    string Title,
    string Slug,
    string? Excerpt,
    string? ContentJson,
    string? ContentHtml,
    string? FeaturedImageUrl,
    string? FeaturedImageAlt,
    string? MetaTitle,
    string? MetaDescription,
    string? CanonicalUrl,
    bool AllowIndexing,
    Guid? CategoryId,
    List<Guid>? TagIds) : IAuditableCommand<PostDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => Id;
    public string? GetTargetDisplayName() => Title;
    public string? GetActionDescription() => $"Updated blog post '{Title}'";
}
