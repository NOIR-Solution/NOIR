namespace NOIR.Application.Features.Pm.Commands.AddSubtask;

public sealed record AddSubtaskCommand(
    Guid ParentTaskId,
    string Title,
    string? Description,
    TaskPriority Priority,
    Guid? AssigneeId) : IAuditableCommand<Features.Pm.DTOs.TaskDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? AuditUserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => ParentTaskId;
    public string? GetTargetDisplayName() => Title;
    public string? GetActionDescription() => $"Added subtask '{Title}'";
}
