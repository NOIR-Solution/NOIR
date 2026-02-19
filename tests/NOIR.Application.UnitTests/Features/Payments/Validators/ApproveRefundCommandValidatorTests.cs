using NOIR.Application.Features.Payments.Commands.ApproveRefund;

namespace NOIR.Application.UnitTests.Features.Payments.Validators;

/// <summary>
/// Unit tests for ApproveRefundCommandValidator.
/// Tests all validation rules for approving a refund.
/// </summary>
public class ApproveRefundCommandValidatorTests
{
    private readonly ApproveRefundCommandValidator _validator = new();

    private static ApproveRefundCommand CreateValidCommand() => new(
        RefundId: Guid.NewGuid(),
        Notes: "Approved after review");

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
        var command = new ApproveRefundCommand(Guid.NewGuid(), null);

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

    #region Notes Validation

    [Fact]
    public async Task Validate_WhenNotesExceeds500Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Notes = new string('A', 501) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Notes)
            .WithErrorMessage("Notes cannot exceed 500 characters.");
    }

    [Fact]
    public async Task Validate_WhenNotesIs500Characters_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Notes = new string('A', 500) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Notes);
    }

    #endregion
}
