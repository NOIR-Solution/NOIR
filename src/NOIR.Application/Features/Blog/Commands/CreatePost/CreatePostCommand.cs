using NOIR.Application.Features.Blog.DTOs;

namespace NOIR.Application.Features.Blog.Commands.CreatePost;

/// <summary>
/// Command to create a new blog post.
/// </summary>
public sealed record CreatePostCommand(
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

    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => UserId;
    public string? GetTargetDisplayName() => Title;
    public string? GetActionDescription() => $"Created blog post '{Title}'";
}
