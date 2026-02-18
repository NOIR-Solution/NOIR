namespace NOIR.Application.Features.Reports.Queries.GetCustomerReport;

/// <summary>
/// Validates the GetCustomerReportQuery parameters.
/// </summary>
public class GetCustomerReportQueryValidator : AbstractValidator<GetCustomerReportQuery>
{
    public GetCustomerReportQueryValidator()
    {
        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithMessage("EndDate must be after StartDate.");
    }
}
