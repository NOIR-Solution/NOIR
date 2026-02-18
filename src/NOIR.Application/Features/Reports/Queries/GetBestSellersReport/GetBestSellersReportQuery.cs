namespace NOIR.Application.Features.Reports.Queries.GetBestSellersReport;

/// <summary>
/// Query to get a best-sellers report for the specified period.
/// </summary>
public sealed record GetBestSellersReportQuery(
    DateTimeOffset? StartDate = null,
    DateTimeOffset? EndDate = null,
    int TopN = 10);
