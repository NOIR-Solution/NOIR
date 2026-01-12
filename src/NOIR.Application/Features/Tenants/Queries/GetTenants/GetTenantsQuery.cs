namespace NOIR.Application.Features.Tenants.Queries.GetTenants;

/// <summary>
/// Query to get all tenants with optional search and filter.
/// </summary>
public sealed record GetTenantsQuery(
    string? Search = null,
    bool? IsActive = null,
    int Page = 1,
    int PageSize = 20);
