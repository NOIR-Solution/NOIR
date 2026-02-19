using NOIR.Application.Features.Reports.Queries.GetRevenueReport;

namespace NOIR.Application.UnitTests.Features.Reports.Validators;

/// <summary>
/// Unit tests for GetRevenueReportQueryValidator.
/// Tests all validation rules for getting a revenue report.
/// </summary>
public class GetRevenueReportQueryValidatorTests
{
    private readonly GetRevenueReportQueryValidator _validator = new();

    #region Period Validation

    [Theory]
    [InlineData("daily")]
    [InlineData("weekly")]
    [InlineData("monthly")]
    public async Task Validate_WhenPeriodIsValid_ShouldNotHaveError(string period)
    {
        // Arrange
        var query = new GetRevenueReportQuery(Period: period);

        // Act
        var result = await _validator.TestValidateAsync(query);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Period);
    }

    [Theory]
    [InlineData("Daily")]
    [InlineData("WEEKLY")]
    [InlineData("Monthly")]
    public async Task Validate_WhenPeriodIsValidWithDifferentCasing_ShouldNotHaveError(string period)
    {
        // Arrange
        var query = new GetRevenueReportQuery(Period: period);

        // Act
        var result = await _validator.TestValidateAsync(query);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Period);
    }

    [Theory]
    [InlineData("yearly")]
    [InlineData("quarterly")]
    [InlineData("hourly")]
    [InlineData("")]
    [InlineData("invalid")]
    public async Task Validate_WhenPeriodIsInvalid_ShouldHaveError(string period)
    {
        // Arrange
        var query = new GetRevenueReportQuery(Period: period);

        // Act
        var result = await _validator.TestValidateAsync(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Period)
            .WithErrorMessage("Period must be 'daily', 'weekly', or 'monthly'.");
    }

    #endregion

    #region Date Range Validation

    [Fact]
    public async Task Validate_WhenEndDateIsBeforeStartDate_ShouldHaveError()
    {
        // Arrange
        var startDate = DateTimeOffset.UtcNow;
        var endDate = startDate.AddDays(-1);
        var query = new GetRevenueReportQuery(
            Period: "monthly",
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
        var query = new GetRevenueReportQuery(
            Period: "monthly",
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
        var query = new GetRevenueReportQuery(
            Period: "monthly",
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
        var query = new GetRevenueReportQuery(
            Period: "monthly",
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
        var query = new GetRevenueReportQuery(
            Period: "monthly",
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
        var query = new GetRevenueReportQuery(
            Period: "monthly",
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
        var query = new GetRevenueReportQuery(
            Period: "monthly",
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
        // Arrange - uses defaults: Period="monthly", no dates
        var query = new GetRevenueReportQuery();

        // Act
        var result = await _validator.TestValidateAsync(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
