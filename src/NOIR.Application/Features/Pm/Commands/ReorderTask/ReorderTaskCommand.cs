namespace NOIR.Application.Features.Pm.Commands.ReorderTask;

public sealed record ReorderTaskCommand(
    Guid TaskId,
    double NewSortOrder) : IAuditableCommand<Features.Pm.DTOs.TaskDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? AuditUserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => TaskId;
    public string? GetTargetDisplayName() => null;
    public string? GetActionDescription() => "Reordered task within column";
}
