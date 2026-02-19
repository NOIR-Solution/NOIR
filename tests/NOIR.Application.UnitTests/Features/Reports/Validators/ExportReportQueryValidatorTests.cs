using NOIR.Application.Features.Reports.DTOs;
using NOIR.Application.Features.Reports.Queries.ExportReport;

namespace NOIR.Application.UnitTests.Features.Reports.Validators;

/// <summary>
/// Unit tests for ExportReportQueryValidator.
/// Tests all validation rules for exporting a report.
/// </summary>
public class ExportReportQueryValidatorTests
{
    private readonly ExportReportQueryValidator _validator = new();

    #region ReportType Validation

    [Theory]
    [InlineData(ReportType.Revenue)]
    [InlineData(ReportType.BestSellers)]
    [InlineData(ReportType.Inventory)]
    [InlineData(ReportType.CustomerAcquisition)]
    public async Task Validate_WhenReportTypeIsValid_ShouldNotHaveError(ReportType reportType)
    {
        // Arrange
        var query = new ExportReportQuery(
            ReportType: reportType,
            Format: ExportFormat.CSV);

        // Act
        var result = await _validator.TestValidateAsync(query);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ReportType);
    }

    [Fact]
    public async Task Validate_WhenReportTypeIsInvalid_ShouldHaveError()
    {
        // Arrange
        var query = new ExportReportQuery(
            ReportType: (ReportType)999,
            Format: ExportFormat.CSV);

        // Act
        var result = await _validator.TestValidateAsync(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ReportType)
            .WithErrorMessage("ReportType must be a valid report type.");
    }

    #endregion

    #region Format Validation

    [Theory]
    [InlineData(ExportFormat.CSV)]
    [InlineData(ExportFormat.Excel)]
    public async Task Validate_WhenFormatIsValid_ShouldNotHaveError(ExportFormat format)
    {
        // Arrange
        var query = new ExportReportQuery(
            ReportType: ReportType.Revenue,
            Format: format);

        // Act
        var result = await _validator.TestValidateAsync(query);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Format);
    }

    [Fact]
    public async Task Validate_WhenFormatIsInvalid_ShouldHaveError()
    {
        // Arrange
        var query = new ExportReportQuery(
            ReportType: ReportType.Revenue,
            Format: (ExportFormat)999);

        // Act
        var result = await _validator.TestValidateAsync(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Format)
            .WithErrorMessage("Format must be CSV or Excel.");
    }

    #endregion

    #region Date Range Validation

    [Fact]
    public async Task Validate_WhenEndDateIsBeforeStartDate_ShouldHaveError()
    {
        // Arrange
        var startDate = DateTimeOffset.UtcNow;
        var endDate = startDate.AddDays(-1);
        var query = new ExportReportQuery(
            ReportType: ReportType.Revenue,
            Format: ExportFormat.CSV,
            StartDate: startDate,
            EndDate: endDate);

        // Act
        var result = await _validator.TestValidateAsync(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EndDate)
            .WithErrorMessage("EndDate must be after StartDate.");
    }

    [Fact]
    public async Task Validate_WhenEndDateEqualsStartDate_ShouldHaveError()
    {
        // Arrange
        var date = DateTimeOffset.UtcNow;
        var query = new ExportReportQuery(
            ReportType: ReportType.Revenue,
            Format: ExportFormat.CSV,
            StartDate: date,
            EndDate: date);

        // Act
        var result = await _validator.TestValidateAsync(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EndDate)
            .WithErrorMessage("EndDate must be after StartDate.");
    }

    [Fact]
    public async Task Validate_WhenEndDateIsAfterStartDate_ShouldNotHaveError()
    {
        // Arrange
        var startDate = DateTimeOffset.UtcNow;
        var endDate = startDate.AddDays(7);
        var query = new ExportReportQuery(
            ReportType: ReportType.Revenue,
            Format: ExportFormat.CSV,
            StartDate: startDate,
            EndDate: endDate);

        // Act
        var result = await _validator.TestValidateAsync(query);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.EndDate);
    }

    [Fact]
    public async Task Validate_WhenBothDatesAreNull_ShouldNotHaveError()
    {
        // Arrange
        var query = new ExportReportQuery(
            ReportType: ReportType.Revenue,
            Format: ExportFormat.CSV,
            StartDate: null,
            EndDate: null);

        // Act
        var result = await _validator.TestValidateAsync(query);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.EndDate);
    }

    [Fact]
    public async Task Validate_WhenOnlyStartDateIsProvided_ShouldNotHaveError()
    {
        // Arrange
        var query = new ExportReportQuery(
            ReportType: ReportType.Revenue,
            Format: ExportFormat.CSV,
            StartDate: DateTimeOffset.UtcNow,
            EndDate: null);

        // Act
        var result = await _validator.TestValidateAsync(query);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.EndDate);
    }

    [Fact]
    public async Task Validate_WhenOnlyEndDateIsProvided_ShouldNotHaveError()
    {
        // Arrange
        var query = new ExportReportQuery(
            ReportType: ReportType.Revenue,
            Format: ExportFormat.CSV,
            StartDate: null,
            EndDate: DateTimeOffset.UtcNow);

        // Act
        var result = await _validator.TestValidateAsync(query);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.EndDate);
    }

    #endregion

    #region Valid Query Tests

    [Fact]
    public async Task Validate_WhenQueryIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var query = new ExportReportQuery(
            ReportType: ReportType.Revenue,
            Format: ExportFormat.CSV,
            StartDate: DateTimeOffset.UtcNow.AddDays(-30),
            EndDate: DateTimeOffset.UtcNow);

        // Act
        var result = await _validator.TestValidateAsync(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenMinimalValidQuery_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var query = new ExportReportQuery(
            ReportType: ReportType.Revenue);

        // Act
        var result = await _validator.TestValidateAsync(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
