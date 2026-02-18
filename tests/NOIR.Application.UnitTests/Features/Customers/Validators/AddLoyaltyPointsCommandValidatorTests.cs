namespace NOIR.Application.UnitTests.Features.Customers.Validators;

/// <summary>
/// Unit tests for AddLoyaltyPointsCommandValidator.
/// Tests all validation rules for adding loyalty points.
/// </summary>
public class AddLoyaltyPointsCommandValidatorTests
{
    private readonly AddLoyaltyPointsCommandValidator _validator = new();

    #region CustomerId Validation

    [Fact]
    public async Task Validate_WhenCustomerIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new AddLoyaltyPointsCommand(Guid.Empty, 100);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CustomerId)
            .WithErrorMessage("Customer ID is required.");
    }

    #endregion

    #region Points Validation

    [Fact]
    public async Task Validate_WhenPointsIsZero_ShouldHaveError()
    {
        // Arrange
        var command = new AddLoyaltyPointsCommand(Guid.NewGuid(), 0);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Points)
            .WithErrorMessage("Points must be greater than zero.");
    }

    [Fact]
    public async Task Validate_WhenPointsIsNegative_ShouldHaveError()
    {
        // Arrange
        var command = new AddLoyaltyPointsCommand(Guid.NewGuid(), -10);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Points)
            .WithErrorMessage("Points must be greater than zero.");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(50000)]
    public async Task Validate_WhenPointsIsPositive_ShouldNotHaveError(int points)
    {
        // Arrange
        var command = new AddLoyaltyPointsCommand(Guid.NewGuid(), points);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Points);
    }

    #endregion

    #region Reason Validation

    [Fact]
    public async Task Validate_WhenReasonExceeds500Characters_ShouldHaveError()
    {
        // Arrange
        var command = new AddLoyaltyPointsCommand(
            Guid.NewGuid(),
            100,
            new string('a', 501));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Reason)
            .WithErrorMessage("Reason cannot exceed 500 characters.");
    }

    [Fact]
    public async Task Validate_WhenReasonIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new AddLoyaltyPointsCommand(Guid.NewGuid(), 100, null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public async Task Validate_WhenReasonIs500Characters_ShouldNotHaveError()
    {
        // Arrange
        var command = new AddLoyaltyPointsCommand(
            Guid.NewGuid(),
            100,
            new string('a', 500));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Reason);
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new AddLoyaltyPointsCommand(
            Guid.NewGuid(),
            500,
            "Purchase reward");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
