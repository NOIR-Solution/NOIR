namespace NOIR.Application.Features.Reports.Queries.GetInventoryReport;

/// <summary>
/// Validates the GetInventoryReportQuery parameters.
/// </summary>
public class GetInventoryReportQueryValidator : AbstractValidator<GetInventoryReportQuery>
{
    public GetInventoryReportQueryValidator()
    {
        RuleFor(x => x.LowStockThreshold)
            .GreaterThan(0)
            .WithMessage("LowStockThreshold must be greater than 0.");
    }
}
