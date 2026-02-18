namespace NOIR.Application.Features.Reports.Queries.ExportReport;

/// <summary>
/// Wolverine handler for exporting a report.
/// </summary>
public class ExportReportQueryHandler
{
    private readonly IReportQueryService _reportService;

    public ExportReportQueryHandler(IReportQueryService reportService)
    {
        _reportService = reportService;
    }

    public async Task<Result<DTOs.ExportResultDto>> Handle(
        ExportReportQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.ExportReportAsync(
            query.ReportType,
            query.Format,
            query.StartDate,
            query.EndDate,
            cancellationToken);

        return Result.Success(result);
    }
}
