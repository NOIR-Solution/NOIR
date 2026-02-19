using NOIR.Application.Features.Reports.Queries.GetCustomerReport;

namespace NOIR.Application.UnitTests.Features.Reports.Validators;

/// <summary>
/// Unit tests for GetCustomerReportQueryValidator.
/// Tests all validation rules for getting a customer report.
/// </summary>
public class GetCustomerReportQueryValidatorTests
{
    private readonly GetCustomerReportQueryValidator _validator = new();

    #region Date Range Validation

    [Fact]
    public async Task Validate_WhenEndDateIsBeforeStartDate_ShouldHaveError()
    {
        // Arrange
        var startDate = DateTimeOffset.UtcNow;
        var endDate = startDate.AddDays(-1);
        var query = new GetCustomerReportQuery(
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
        var query = new GetCustomerReportQuery(
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
        var query = new GetCustomerReportQuery(
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
        var query = new GetCustomerReportQuery(
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
        var query = new GetCustomerReportQuery(
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
        var query = new GetCustomerReportQuery(
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
        var query = new GetCustomerReportQuery(
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
        // Arrange - uses defaults: no dates
        var query = new GetCustomerReportQuery();

        // Act
        var result = await _validator.TestValidateAsync(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
