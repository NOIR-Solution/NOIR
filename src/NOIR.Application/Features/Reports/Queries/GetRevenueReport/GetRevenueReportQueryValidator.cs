namespace NOIR.Application.Features.Reports.Queries.GetRevenueReport;

/// <summary>
/// Validates the GetRevenueReportQuery parameters.
/// </summary>
public class GetRevenueReportQueryValidator : AbstractValidator<GetRevenueReportQuery>
{
    private static readonly string[] ValidPeriods = ["daily", "weekly", "monthly"];

    public GetRevenueReportQueryValidator()
    {
        RuleFor(x => x.Period)
            .Must(p => ValidPeriods.Contains(p.ToLowerInvariant()))
            .WithMessage("Period must be 'daily', 'weekly', or 'monthly'.");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithMessage("EndDate must be after StartDate.");
    }
}
