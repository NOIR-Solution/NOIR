using NOIR.Application.Features.Reports.Queries.GetInventoryReport;

namespace NOIR.Application.UnitTests.Features.Reports.Validators;

/// <summary>
/// Unit tests for GetInventoryReportQueryValidator.
/// Tests all validation rules for getting an inventory report.
/// </summary>
public class GetInventoryReportQueryValidatorTests
{
    private readonly GetInventoryReportQueryValidator _validator = new();

    #region LowStockThreshold Validation

    [Fact]
    public async Task Validate_WhenLowStockThresholdIsZero_ShouldHaveError()
    {
        // Arrange
        var query = new GetInventoryReportQuery(LowStockThreshold: 0);

        // Act
        var result = await _validator.TestValidateAsync(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LowStockThreshold)
            .WithErrorMessage("LowStockThreshold must be greater than 0.");
    }

    [Fact]
    public async Task Validate_WhenLowStockThresholdIsNegative_ShouldHaveError()
    {
        // Arrange
        var query = new GetInventoryReportQuery(LowStockThreshold: -5);

        // Act
        var result = await _validator.TestValidateAsync(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LowStockThreshold)
            .WithErrorMessage("LowStockThreshold must be greater than 0.");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    public async Task Validate_WhenLowStockThresholdIsPositive_ShouldNotHaveError(int threshold)
    {
        // Arrange
        var query = new GetInventoryReportQuery(LowStockThreshold: threshold);

        // Act
        var result = await _validator.TestValidateAsync(query);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.LowStockThreshold);
    }

    #endregion

    #region Valid Query Tests

    [Fact]
    public async Task Validate_WhenQueryIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var query = new GetInventoryReportQuery(LowStockThreshold: 20);

        // Act
        var result = await _validator.TestValidateAsync(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenMinimalValidQuery_ShouldNotHaveAnyErrors()
    {
        // Arrange - uses default: LowStockThreshold=10
        var query = new GetInventoryReportQuery();

        // Act
        var result = await _validator.TestValidateAsync(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
