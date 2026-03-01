namespace NOIR.Application.Features.Hr.Commands.CreateTag;

public sealed record CreateTagCommand(
    string Name,
    EmployeeTagCategory Category,
    string? Color = null,
    string? Description = null,
    int SortOrder = 0) : IAuditableCommand<EmployeeTagDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => UserId;
    public string? GetTargetDisplayName() => Name;
    public string? GetActionDescription() => $"Created employee tag '{Name}'";
}
