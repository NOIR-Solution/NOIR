namespace NOIR.Application.Features.Reports.Queries.GetInventoryReport;

/// <summary>
/// Query to get an inventory health report.
/// </summary>
public sealed record GetInventoryReportQuery(
    int LowStockThreshold = 10);
