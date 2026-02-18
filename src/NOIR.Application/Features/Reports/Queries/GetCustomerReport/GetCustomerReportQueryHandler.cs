namespace NOIR.Application.Features.Reports.Queries.GetCustomerReport;

/// <summary>
/// Wolverine handler for getting the customer report.
/// </summary>
public class GetCustomerReportQueryHandler
{
    private readonly IReportQueryService _reportService;

    public GetCustomerReportQueryHandler(IReportQueryService reportService)
    {
        _reportService = reportService;
    }

    public async Task<Result<DTOs.CustomerReportDto>> Handle(
        GetCustomerReportQuery query,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var startDate = query.StartDate ?? now.AddMonths(-12);
        var endDate = query.EndDate ?? now;

        var report = await _reportService.GetCustomerReportAsync(
            startDate,
            endDate,
            cancellationToken);

        return Result.Success(report);
    }
}
