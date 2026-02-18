namespace NOIR.Application.Features.Customers.Queries.GetCustomerStats;

/// <summary>
/// Query to get customer statistics for dashboards/charts.
/// </summary>
public sealed record GetCustomerStatsQuery(int TopSpendersCount = 10);
