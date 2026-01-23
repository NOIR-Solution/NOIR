
namespace NOIR.Application.Features.Blog.Commands.UpdateTag;

/// <summary>
/// Command to update an existing blog tag.
/// </summary>
public sealed record UpdateTagCommand(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? Color) : IAuditableCommand<PostTagDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => Id;
    public string? GetTargetDisplayName() => Name;
    public string? GetActionDescription() => $"Updated blog tag '{Name}'";
}
