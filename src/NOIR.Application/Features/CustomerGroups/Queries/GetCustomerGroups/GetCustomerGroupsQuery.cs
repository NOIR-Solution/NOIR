namespace NOIR.Application.Features.CustomerGroups.Queries.GetCustomerGroups;

/// <summary>
/// Query to get paged list of customer groups.
/// </summary>
public sealed record GetCustomerGroupsQuery(
    string? Search = null,
    bool? IsActive = null,
    int Page = 1,
    int PageSize = 20);
