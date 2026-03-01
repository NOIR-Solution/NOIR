namespace NOIR.Application.Features.Hr.Commands.BulkAssignTags;

/// <summary>
/// Command to bulk assign tags to multiple employees.
/// </summary>
public sealed record BulkAssignTagsCommand(
    List<Guid> EmployeeIds,
    List<Guid> TagIds) : IAuditableCommand<BulkOperationResultDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => EmployeeIds.Count == 1 ? EmployeeIds[0] : string.Join(",", EmployeeIds.Take(5));
    public string? GetTargetDisplayName() => $"{EmployeeIds.Count} employees";
    public string? GetActionDescription() => $"Bulk assigned {TagIds.Count} tag(s) to {EmployeeIds.Count} employee(s)";
}
