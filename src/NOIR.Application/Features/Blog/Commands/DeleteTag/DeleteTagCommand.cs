namespace NOIR.Application.Features.Blog.Commands.DeleteTag;

/// <summary>
/// Command to soft delete a blog tag.
/// </summary>
public sealed record DeleteTagCommand(
    Guid Id,
    string? TagName = null) : IAuditableCommand
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Delete;
    public object? GetTargetId() => Id;
    public string? GetTargetDisplayName() => TagName ?? Id.ToString();
    public string? GetActionDescription() => $"Deleted blog tag '{GetTargetDisplayName()}'";
}
