namespace NOIR.Application.Features.Pm.Commands.ChangeProjectStatus;

public sealed record ChangeProjectStatusCommand(
    Guid ProjectId,
    ProjectStatus NewStatus) : IAuditableCommand<Features.Pm.DTOs.ProjectDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? AuditUserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => ProjectId;
    public string? GetTargetDisplayName() => null;
    public string? GetActionDescription() => $"Changed project status to {NewStatus}";
}
