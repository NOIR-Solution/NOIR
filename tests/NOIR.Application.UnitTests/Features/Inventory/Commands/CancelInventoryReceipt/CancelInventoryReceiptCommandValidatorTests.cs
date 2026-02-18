using NOIR.Application.Features.Inventory.Commands.CancelInventoryReceipt;

namespace NOIR.Application.UnitTests.Features.Inventory.Commands.CancelInventoryReceipt;

/// <summary>
/// Unit tests for CancelInventoryReceiptCommandValidator.
/// </summary>
public class CancelInventoryReceiptCommandValidatorTests
{
    private readonly CancelInventoryReceiptCommandValidator _validator;

    public CancelInventoryReceiptCommandValidatorTests()
    {
        _validator = new CancelInventoryReceiptCommandValidator();
    }

    [Fact]
    public async Task Validate_WithValidCommand_ShouldPass()
    {
        // Arrange
        var command = new CancelInventoryReceiptCommand(Guid.NewGuid(), "No longer needed");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithEmptyReceiptId_ShouldFail()
    {
        // Arrange
        var command = new CancelInventoryReceiptCommand(Guid.Empty, "Reason");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ReceiptId)
            .WithErrorMessage("Receipt ID is required.");
    }

    [Fact]
    public async Task Validate_WithNullReason_ShouldPass()
    {
        // Arrange
        var command = new CancelInventoryReceiptCommand(Guid.NewGuid(), null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithReasonExceeding500Characters_ShouldFail()
    {
        // Arrange
        var command = new CancelInventoryReceiptCommand(Guid.NewGuid(), new string('A', 501));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Reason)
            .WithErrorMessage("Cancellation reason cannot exceed 500 characters.");
    }

    [Fact]
    public async Task Validate_WithReasonExactly500Characters_ShouldPass()
    {
        // Arrange
        var command = new CancelInventoryReceiptCommand(Guid.NewGuid(), new string('A', 500));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
