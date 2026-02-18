namespace NOIR.Application.Features.Reports.Queries.GetRevenueReport;

/// <summary>
/// Query to get a revenue report for the specified period.
/// </summary>
public sealed record GetRevenueReportQuery(
    string Period = "monthly",
    DateTimeOffset? StartDate = null,
    DateTimeOffset? EndDate = null);
