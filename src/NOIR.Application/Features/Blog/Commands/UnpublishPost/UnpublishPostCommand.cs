
namespace NOIR.Application.Features.Blog.Commands.UnpublishPost;

/// <summary>
/// Command to unpublish a blog post (revert to draft).
/// </summary>
public sealed record UnpublishPostCommand(
    Guid Id,
    string? PostTitle = null) : IAuditableCommand<PostDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => Id;
    public string? GetTargetDisplayName() => PostTitle ?? Id.ToString();
    public string? GetActionDescription() => $"Unpublished blog post '{GetTargetDisplayName()}'";
}
