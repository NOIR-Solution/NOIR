namespace NOIR.Application.Features.Hr.Commands.RemoveTagsFromEmployee;

public sealed record RemoveTagsFromEmployeeCommand(
    Guid EmployeeId,
    List<Guid> TagIds) : IAuditableCommand<List<TagBriefDto>>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => EmployeeId;
    public string? GetTargetDisplayName() => EmployeeId.ToString();
    public string? GetActionDescription() => $"Removed {TagIds.Count} tag(s) from employee";
}
