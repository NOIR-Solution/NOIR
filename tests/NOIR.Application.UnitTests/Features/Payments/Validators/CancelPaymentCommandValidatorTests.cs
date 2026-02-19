using NOIR.Application.Features.Payments.Commands.CancelPayment;

namespace NOIR.Application.UnitTests.Features.Payments.Validators;

/// <summary>
/// Unit tests for CancelPaymentCommandValidator.
/// Tests all validation rules for cancelling a payment.
/// </summary>
public class CancelPaymentCommandValidatorTests
{
    private readonly CancelPaymentCommandValidator _validator = new();

    private static CancelPaymentCommand CreateValidCommand() => new(
        PaymentTransactionId: Guid.NewGuid(),
        Reason: "Customer requested cancellation");

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
        var command = new CancelPaymentCommand(Guid.NewGuid(), null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region PaymentTransactionId Validation

    [Fact]
    public async Task Validate_WhenPaymentTransactionIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { PaymentTransactionId = Guid.Empty };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PaymentTransactionId)
            .WithErrorMessage("Payment transaction ID is required.");
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
            .WithErrorMessage("Reason cannot exceed 500 characters.");
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
