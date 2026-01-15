namespace NOIR.Application.Features.Roles.Queries.GetRoles;

/// <summary>
/// Query to get all roles with optional search and tenant filtering.
/// </summary>
public sealed record GetRolesQuery(
    string? Search = null,
    int Page = 1,
    int PageSize = 20,
    Guid? TenantId = null,
    bool IncludeSystemRoles = true);
