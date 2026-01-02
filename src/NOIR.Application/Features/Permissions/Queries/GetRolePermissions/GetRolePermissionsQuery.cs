namespace NOIR.Application.Features.Permissions.Queries.GetRolePermissions;

/// <summary>
/// Query to get all permissions assigned to a role.
/// </summary>
public sealed record GetRolePermissionsQuery(string RoleId);
