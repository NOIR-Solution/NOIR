namespace NOIR.Application.Features.Reports.Queries.GetInventoryReport;

/// <summary>
/// Wolverine handler for getting the inventory report.
/// </summary>
public class GetInventoryReportQueryHandler
{
    private readonly IReportQueryService _reportService;

    public GetInventoryReportQueryHandler(IReportQueryService reportService)
    {
        _reportService = reportService;
    }

    public async Task<Result<DTOs.InventoryReportDto>> Handle(
        GetInventoryReportQuery query,
        CancellationToken cancellationToken)
    {
        var report = await _reportService.GetInventoryReportAsync(
            query.LowStockThreshold,
            cancellationToken);

        return Result.Success(report);
    }
}
