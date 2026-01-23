
namespace NOIR.Application.Features.Blog.Commands.CreateTag;

/// <summary>
/// Command to create a new blog tag.
/// </summary>
public sealed record CreateTagCommand(
    string Name,
    string Slug,
    string? Description,
    string? Color) : IAuditableCommand<PostTagDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => UserId;
    public string? GetTargetDisplayName() => Name;
    public string? GetActionDescription() => $"Created blog tag '{Name}'";
}
