namespace NOIR.Application.Features.Dashboard.Queries.GetDashboardMetrics;

/// <summary>
/// Query to get aggregated dashboard metrics.
/// </summary>
public sealed record GetDashboardMetricsQuery(
    int TopProductsCount = 5,
    int LowStockThreshold = 10,
    int RecentOrdersCount = 10,
    int SalesOverTimeDays = 30);
