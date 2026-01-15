namespace NOIR.Application.Features.Roles.Commands.CreateRole;

/// <summary>
/// Command to create a new role with hierarchy and tenant support.
/// </summary>
public sealed record CreateRoleCommand(
    string Name,
    string? Description = null,
    string? ParentRoleId = null,
    Guid? TenantId = null,
    int SortOrder = 0,
    string? IconName = null,
    string? Color = null,
    IReadOnlyList<string>? Permissions = null) : IAuditableCommand
{
    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => Name;
}
