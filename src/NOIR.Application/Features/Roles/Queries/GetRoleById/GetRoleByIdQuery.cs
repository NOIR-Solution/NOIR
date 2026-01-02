namespace NOIR.Application.Features.Roles.Queries.GetRoleById;

/// <summary>
/// Query to get a role by ID with its permissions.
/// </summary>
public sealed record GetRoleByIdQuery(string RoleId);
