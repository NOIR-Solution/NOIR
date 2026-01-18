namespace NOIR.Application.Features.Blog.Commands.DeletePost;

/// <summary>
/// Command to soft delete a blog post.
/// </summary>
public sealed record DeletePostCommand(
    Guid Id,
    string? PostTitle = null) : IAuditableCommand
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Delete;
    public object? GetTargetId() => Id;
    public string? GetTargetDisplayName() => PostTitle ?? Id.ToString();
    public string? GetActionDescription() => $"Deleted blog post '{GetTargetDisplayName()}'";
}
