using NOIR.Application.Features.Payments.Commands.RejectRefund;

namespace NOIR.Application.UnitTests.Features.Payments.Validators;

/// <summary>
/// Unit tests for RejectRefundCommandValidator.
/// Tests all validation rules for rejecting a refund.
/// </summary>
public class RejectRefundCommandValidatorTests
{
    private readonly RejectRefundCommandValidator _validator = new();

    private static RejectRefundCommand CreateValidCommand() => new(
        RefundId: Guid.NewGuid(),
        RejectionReason: "Refund period has expired");

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

    #endregion

    #region RefundId Validation

    [Fact]
    public async Task Validate_WhenRefundIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { RefundId = Guid.Empty };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RefundId)
            .WithErrorMessage("Refund ID is required.");
    }

    #endregion

    #region RejectionReason Validation

    [Fact]
    public async Task Validate_WhenRejectionReasonIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { RejectionReason = "" };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RejectionReason)
            .WithErrorMessage("Rejection reason is required.");
    }

    [Fact]
    public async Task Validate_WhenRejectionReasonExceeds1000Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { RejectionReason = new string('A', 1001) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RejectionReason)
            .WithErrorMessage("Rejection reason cannot exceed 1000 characters.");
    }

    [Fact]
    public async Task Validate_WhenRejectionReasonIs1000Characters_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { RejectionReason = new string('A', 1000) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.RejectionReason);
    }

    #endregion
}
