namespace NOIR.Application.Features.Hr.Commands.BulkChangeDepartment;

/// <summary>
/// Command to bulk change department for multiple employees.
/// </summary>
public sealed record BulkChangeDepartmentCommand(
    List<Guid> EmployeeIds,
    Guid NewDepartmentId) : IAuditableCommand<BulkOperationResultDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => EmployeeIds.Count == 1 ? EmployeeIds[0] : string.Join(",", EmployeeIds.Take(5));
    public string? GetTargetDisplayName() => $"{EmployeeIds.Count} employees";
    public string? GetActionDescription() => $"Bulk changed department to {NewDepartmentId} for {EmployeeIds.Count} employee(s)";
}
