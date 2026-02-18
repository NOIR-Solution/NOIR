namespace NOIR.Application.Features.Reports.Queries.ExportReport;

/// <summary>
/// Query to export a report as a file (CSV or Excel).
/// </summary>
public sealed record ExportReportQuery(
    DTOs.ReportType ReportType,
    DTOs.ExportFormat Format = DTOs.ExportFormat.CSV,
    DateTimeOffset? StartDate = null,
    DateTimeOffset? EndDate = null);
