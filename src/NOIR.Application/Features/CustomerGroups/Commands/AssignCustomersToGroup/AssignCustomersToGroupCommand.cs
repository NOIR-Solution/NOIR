namespace NOIR.Application.Features.CustomerGroups.Commands.AssignCustomersToGroup;

/// <summary>
/// Command to assign customers to a group.
/// </summary>
public sealed record AssignCustomersToGroupCommand(
    Guid CustomerGroupId,
    List<Guid> CustomerIds) : IAuditableCommand
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => CustomerGroupId;
    public string? GetTargetDisplayName() => $"Group {CustomerGroupId}";
    public string? GetActionDescription() => $"Assigned {CustomerIds.Count} customer(s) to group";
}
