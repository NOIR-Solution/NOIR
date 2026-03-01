namespace NOIR.Application.Features.Hr.Commands.UpdateTag;

public sealed record UpdateTagCommand(
    Guid Id,
    string Name,
    EmployeeTagCategory Category,
    string? Color = null,
    string? Description = null,
    int SortOrder = 0) : IAuditableCommand<EmployeeTagDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => Id;
    public string? GetTargetDisplayName() => Name;
    public string? GetActionDescription() => $"Updated employee tag '{Name}'";
}
