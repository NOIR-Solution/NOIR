using NOIR.Application.Features.Payments.Commands.RequestRefund;

namespace NOIR.Application.UnitTests.Features.Payments.Validators;

/// <summary>
/// Unit tests for RequestRefundCommandValidator.
/// Tests all validation rules for requesting a payment refund.
/// </summary>
public class RequestRefundCommandValidatorTests
{
    private readonly RequestRefundCommandValidator _validator = new();

    private static RequestRefundCommand CreateValidCommand() => new(
        PaymentTransactionId: Guid.NewGuid(),
        Amount: 50.00m,
        Reason: RefundReason.CustomerRequest,
        Notes: "Customer changed their mind");

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
    public async Task Validate_WhenNotesIsNull_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = CreateValidCommand() with { Notes = null };

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

    #region Amount Validation

    [Fact]
    public async Task Validate_WhenAmountIsZero_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Amount = 0 };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Amount)
            .WithErrorMessage("Refund amount must be greater than zero.");
    }

    [Fact]
    public async Task Validate_WhenAmountIsNegative_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Amount = -5m };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Amount)
            .WithErrorMessage("Refund amount must be greater than zero.");
    }

    #endregion

    #region Reason Validation

    [Fact]
    public async Task Validate_WhenReasonIsInvalid_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Reason = (RefundReason)999 };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Reason)
            .WithErrorMessage("Invalid refund reason.");
    }

    [Fact]
    public async Task Validate_WhenReasonIsValid_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Reason = RefundReason.Defective };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Reason);
    }

    #endregion

    #region Notes Validation

    [Fact]
    public async Task Validate_WhenNotesExceeds1000Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Notes = new string('A', 1001) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Notes)
            .WithErrorMessage("Notes cannot exceed 1000 characters.");
    }

    [Fact]
    public async Task Validate_WhenNotesIs1000Characters_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Notes = new string('A', 1000) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Notes);
    }

    #endregion
}
