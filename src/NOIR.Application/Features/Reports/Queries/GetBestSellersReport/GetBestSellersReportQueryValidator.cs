namespace NOIR.Application.Features.Reports.Queries.GetBestSellersReport;

/// <summary>
/// Validates the GetBestSellersReportQuery parameters.
/// </summary>
public class GetBestSellersReportQueryValidator : AbstractValidator<GetBestSellersReportQuery>
{
    public GetBestSellersReportQueryValidator()
    {
        RuleFor(x => x.TopN)
            .GreaterThan(0)
            .WithMessage("TopN must be greater than 0.")
            .LessThanOrEqualTo(100)
            .WithMessage("TopN must not exceed 100.");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithMessage("EndDate must be after StartDate.");
    }
}
