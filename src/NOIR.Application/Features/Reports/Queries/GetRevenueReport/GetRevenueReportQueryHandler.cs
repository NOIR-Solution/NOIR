namespace NOIR.Application.Features.Reports.Queries.GetRevenueReport;

/// <summary>
/// Wolverine handler for getting the revenue report.
/// </summary>
public class GetRevenueReportQueryHandler
{
    private readonly IReportQueryService _reportService;

    public GetRevenueReportQueryHandler(IReportQueryService reportService)
    {
        _reportService = reportService;
    }

    public async Task<Result<DTOs.RevenueReportDto>> Handle(
        GetRevenueReportQuery query,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var startDate = query.StartDate ?? now.AddDays(-30);
        var endDate = query.EndDate ?? now;

        var report = await _reportService.GetRevenueReportAsync(
            query.Period,
            startDate,
            endDate,
            cancellationToken);

        return Result.Success(report);
    }
}
