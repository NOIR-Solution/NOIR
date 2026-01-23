
namespace NOIR.Application.Features.Blog.Commands.PublishPost;

/// <summary>
/// Command to publish or schedule a blog post.
/// </summary>
public sealed record PublishPostCommand(
    Guid Id,
    DateTimeOffset? ScheduledPublishAt = null,
    string? PostTitle = null) : IAuditableCommand<PostDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => Id;
    public string? GetTargetDisplayName() => PostTitle ?? Id.ToString();
    public string? GetActionDescription() => ScheduledPublishAt.HasValue
        ? $"Scheduled blog post '{GetTargetDisplayName()}' for {ScheduledPublishAt:yyyy-MM-dd HH:mm}"
        : $"Published blog post '{GetTargetDisplayName()}'";
}
