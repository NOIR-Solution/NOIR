namespace NOIR.Application.Features.Hr.Commands.AssignTagsToEmployee;

public sealed record AssignTagsToEmployeeCommand(
    Guid EmployeeId,
    List<Guid> TagIds) : IAuditableCommand<List<TagBriefDto>>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => EmployeeId;
    public string? GetTargetDisplayName() => EmployeeId.ToString();
    public string? GetActionDescription() => $"Assigned {TagIds.Count} tag(s) to employee";
}
