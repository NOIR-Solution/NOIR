using NOIR.Application.Features.Reports.Queries.GetBestSellersReport;

namespace NOIR.Application.UnitTests.Features.Reports.Validators;

/// <summary>
/// Unit tests for GetBestSellersReportQueryValidator.
/// Tests all validation rules for getting a best-sellers report.
/// </summary>
public class GetBestSellersReportQueryValidatorTests
{
    private readonly GetBestSellersReportQueryValidator _validator = new();

    #region TopN Validation

    [Fact]
    public async Task Validate_WhenTopNIsZero_ShouldHaveError()
    {
        // Arrange
        var query = new GetBestSellersReportQuery(TopN: 0);

        // Act
        var result = await _validator.TestValidateAsync(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TopN)
            .WithErrorMessage("TopN must be greater than 0.");
    }

    [Fact]
    public async Task Validate_WhenTopNIsNegative_ShouldHaveError()
    {
        // Arrange
        var query = new GetBestSellersReportQuery(TopN: -1);

        // Act
        var result = await _validator.TestValidateAsync(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TopN)
            .WithErrorMessage("TopN must be greater than 0.");
    }

    [Fact]
    public async Task Validate_WhenTopNExceeds100_ShouldHaveError()
    {
        // Arrange
        var query = new GetBestSellersReportQuery(TopN: 101);

        // Act
        var result = await _validator.TestValidateAsync(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TopN)
            .WithErrorMessage("TopN must not exceed 100.");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    public async Task Validate_WhenTopNIsValid_ShouldNotHaveError(int topN)
    {
        // Arrange
        var query = new GetBestSellersReportQuery(TopN: topN);

        // Act
        var result = await _validator.TestValidateAsync(query);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TopN);
    }

    #endregion

    #region Date Range Validation

    [Fact]
    public async Task Validate_WhenEndDateIsBeforeStartDate_ShouldHaveError()
    {
        // Arrange
        var startDate = DateTimeOffset.UtcNow;
        var endDate = startDate.AddDays(-1);
        var query = new GetBestSellersReportQuery(
            StartDate: startDate,
            EndDate: endDate,
            TopN: 10);

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
        var query = new GetBestSellersReportQuery(
            StartDate: date,
            EndDate: date,
            TopN: 10);

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
        var query = new GetBestSellersReportQuery(
            StartDate: startDate,
            EndDate: endDate,
            TopN: 10);

        // Act
        var result = await _validator.TestValidateAsync(query);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.EndDate);
    }

    [Fact]
    public async Task Validate_WhenBothDatesAreNull_ShouldNotHaveError()
    {
        // Arrange
        var query = new GetBestSellersReportQuery(
            StartDate: null,
            EndDate: null,
            TopN: 10);

        // Act
        var result = await _validator.TestValidateAsync(query);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.EndDate);
    }

    [Fact]
    public async Task Validate_WhenOnlyStartDateIsProvided_ShouldNotHaveError()
    {
        // Arrange
        var query = new GetBestSellersReportQuery(
            StartDate: DateTimeOffset.UtcNow,
            EndDate: null,
            TopN: 10);

        // Act
        var result = await _validator.TestValidateAsync(query);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.EndDate);
    }

    [Fact]
    public async Task Validate_WhenOnlyEndDateIsProvided_ShouldNotHaveError()
    {
        // Arrange
        var query = new GetBestSellersReportQuery(
            StartDate: null,
            EndDate: DateTimeOffset.UtcNow,
            TopN: 10);

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
        var query = new GetBestSellersReportQuery(
            StartDate: DateTimeOffset.UtcNow.AddDays(-30),
            EndDate: DateTimeOffset.UtcNow,
            TopN: 20);

        // Act
        var result = await _validator.TestValidateAsync(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenMinimalValidQuery_ShouldNotHaveAnyErrors()
    {
        // Arrange - uses defaults: TopN=10, no dates
        var query = new GetBestSellersReportQuery();

        // Act
        var result = await _validator.TestValidateAsync(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
