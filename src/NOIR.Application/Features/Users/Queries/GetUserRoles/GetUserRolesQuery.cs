namespace NOIR.Application.Features.Users.Queries.GetUserRoles;

/// <summary>
/// Query to get roles assigned to a user.
/// </summary>
public sealed record GetUserRolesQuery(string UserId);
