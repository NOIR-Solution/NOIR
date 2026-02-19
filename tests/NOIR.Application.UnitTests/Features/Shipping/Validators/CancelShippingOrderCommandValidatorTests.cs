namespace NOIR.Application.UnitTests.Features.Shipping.Validators;

/// <summary>
/// Unit tests for CancelShippingOrderCommandValidator.
/// Tests all validation rules for cancelling a shipping order.
/// </summary>
public class CancelShippingOrderCommandValidatorTests
{
    private readonly CancelShippingOrderCommandValidator _validator = new();

    private static CancelShippingOrderCommand CreateValidCommand() => new(
        TrackingNumber: "GHTK-123456",
        Reason: "Customer cancelled order");

    #region Valid Command

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenReasonIsNull_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new CancelShippingOrderCommand("GHTK-123456", null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region TrackingNumber Validation

    [Fact]
    public async Task Validate_WhenTrackingNumberIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { TrackingNumber = "" };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TrackingNumber)
            .WithErrorMessage("Tracking number is required.");
    }

    #endregion

    #region Reason Validation

    [Fact]
    public async Task Validate_WhenReasonExceeds500Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Reason = new string('A', 501) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Reason)
            .WithErrorMessage("Reason must not exceed 500 characters.");
    }

    [Fact]
    public async Task Validate_WhenReasonIs500Characters_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Reason = new string('A', 500) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Reason);
    }

    #endregion
}
