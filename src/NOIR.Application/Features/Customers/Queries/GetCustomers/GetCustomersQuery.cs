namespace NOIR.Application.Features.Customers.Queries.GetCustomers;

/// <summary>
/// Query to get customers with pagination and filtering.
/// </summary>
public sealed record GetCustomersQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    CustomerSegment? Segment = null,
    CustomerTier? Tier = null,
    bool? IsActive = null,
    string? SortBy = null,
    bool SortDescending = true);
