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
    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => RoleId;
}
