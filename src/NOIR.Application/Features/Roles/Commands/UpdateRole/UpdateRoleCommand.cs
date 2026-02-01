namespace NOIR.Application.Features.Roles.Commands.UpdateRole;

/// <summary>
/// Command to update a role with all configurable properties.
/// </summary>
public sealed record UpdateRoleCommand(
    string RoleId,
    string Name,
    string? Description = null,
    string? ParentRoleId = null,
    int SortOrder = 0,
    string? IconName = null,
    string? Color = null) : IAuditableCommand
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => RoleId;
    public string? GetTargetDisplayName() => Name;
    public string? GetActionDescription() => $"Updated role '{Name}'";
}
