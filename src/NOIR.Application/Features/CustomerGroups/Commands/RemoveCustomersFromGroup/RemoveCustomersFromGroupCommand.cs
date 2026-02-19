namespace NOIR.Application.Features.CustomerGroups.Commands.RemoveCustomersFromGroup;

/// <summary>
/// Command to remove customers from a group.
/// </summary>
public sealed record RemoveCustomersFromGroupCommand(
    Guid CustomerGroupId,
    List<Guid> CustomerIds) : IAuditableCommand
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => CustomerGroupId;
    public string? GetTargetDisplayName() => $"Group {CustomerGroupId}";
    public string? GetActionDescription() => $"Removed {CustomerIds.Count} customer(s) from group";
}
