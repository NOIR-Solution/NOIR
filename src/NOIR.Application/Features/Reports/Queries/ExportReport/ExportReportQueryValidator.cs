namespace NOIR.Application.Features.Reports.Queries.ExportReport;

/// <summary>
/// Validates the ExportReportQuery parameters.
/// </summary>
public class ExportReportQueryValidator : AbstractValidator<ExportReportQuery>
{
    public ExportReportQueryValidator()
    {
        RuleFor(x => x.ReportType)
            .IsInEnum()
            .WithMessage("ReportType must be a valid report type.");

        RuleFor(x => x.Format)
            .IsInEnum()
            .WithMessage("Format must be CSV or Excel.");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithMessage("EndDate must be after StartDate.");
    }
}
