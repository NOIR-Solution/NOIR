using NOIR.Application.Features.Inventory.Commands.ConfirmInventoryReceipt;

namespace NOIR.Application.UnitTests.Features.Inventory.Commands.ConfirmInventoryReceipt;

/// <summary>
/// Unit tests for ConfirmInventoryReceiptCommandValidator.
/// </summary>
public class ConfirmInventoryReceiptCommandValidatorTests
{
    private readonly ConfirmInventoryReceiptCommandValidator _validator;

    public ConfirmInventoryReceiptCommandValidatorTests()
    {
        _validator = new ConfirmInventoryReceiptCommandValidator();
    }

    [Fact]
    public async Task Validate_WithValidReceiptId_ShouldPass()
    {
        // Arrange
        var command = new ConfirmInventoryReceiptCommand(Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithEmptyReceiptId_ShouldFail()
    {
        // Arrange
        var command = new ConfirmInventoryReceiptCommand(Guid.Empty);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ReceiptId)
            .WithErrorMessage("Receipt ID is required.");
    }
}
