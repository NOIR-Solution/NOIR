namespace NOIR.Application.Features.Roles.Queries.GetRoles;

/// <summary>
/// Query to get all roles with optional search.
/// </summary>
public sealed record GetRolesQuery(
    string? Search = null,
    int Page = 1,
    int PageSize = 20);
