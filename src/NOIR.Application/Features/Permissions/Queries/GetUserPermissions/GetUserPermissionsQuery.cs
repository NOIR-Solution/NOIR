namespace NOIR.Application.Features.Permissions.Queries.GetUserPermissions;

/// <summary>
/// Query to get effective permissions for a user (combined from all roles).
/// </summary>
public sealed record GetUserPermissionsQuery(string UserId);
