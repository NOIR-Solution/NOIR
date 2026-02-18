namespace NOIR.Application.Features.Reports.Queries.GetBestSellersReport;

/// <summary>
/// Wolverine handler for getting the best-sellers report.
/// </summary>
public class GetBestSellersReportQueryHandler
{
    private readonly IReportQueryService _reportService;

    public GetBestSellersReportQueryHandler(IReportQueryService reportService)
    {
        _reportService = reportService;
    }

    public async Task<Result<DTOs.BestSellersReportDto>> Handle(
        GetBestSellersReportQuery query,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var startDate = query.StartDate ?? now.AddDays(-30);
        var endDate = query.EndDate ?? now;

        var report = await _reportService.GetBestSellersAsync(
            startDate,
            endDate,
            query.TopN,
            cancellationToken);

        return Result.Success(report);
    }
}
