namespace NOIR.Application.Features.Reports.Queries.GetCustomerReport;

/// <summary>
/// Query to get a customer acquisition and retention report.
/// </summary>
public sealed record GetCustomerReportQuery(
    DateTimeOffset? StartDate = null,
    DateTimeOffset? EndDate = null);
