using NOIR.Application.Features.Inventory.Commands.CreateStockMovement;

namespace NOIR.Application.UnitTests.Features.Inventory.Commands.CreateStockMovement;

/// <summary>
/// Unit tests for CreateStockMovementCommandValidator.
/// </summary>
public class CreateStockMovementCommandValidatorTests
{
    private readonly CreateStockMovementCommandValidator _validator;

    public CreateStockMovementCommandValidatorTests()
    {
        _validator = new CreateStockMovementCommandValidator();
    }

    [Fact]
    public async Task Validate_WithValidCommand_ShouldPass()
    {
        // Arrange
        var command = new CreateStockMovementCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            InventoryMovementType.StockIn,
            10,
            "PO-001",
            "Supplier delivery");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithEmptyProductId_ShouldFail()
    {
        // Arrange
        var command = new CreateStockMovementCommand(
            Guid.Empty,
            Guid.NewGuid(),
            InventoryMovementType.StockIn,
            10,
            null,
            null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProductId)
            .WithErrorMessage("Product ID is required.");
    }

    [Fact]
    public async Task Validate_WithEmptyVariantId_ShouldFail()
    {
        // Arrange
        var command = new CreateStockMovementCommand(
            Guid.NewGuid(),
            Guid.Empty,
            InventoryMovementType.StockIn,
            10,
            null,
            null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProductVariantId)
            .WithErrorMessage("Product Variant ID is required.");
    }

    [Fact]
    public async Task Validate_WithZeroQuantity_ShouldFail()
    {
        // Arrange
        var command = new CreateStockMovementCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            InventoryMovementType.StockIn,
            0,
            null,
            null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Quantity)
            .WithErrorMessage("Quantity cannot be zero.");
    }

    [Fact]
    public async Task Validate_WithReferenceExceeding100Characters_ShouldFail()
    {
        // Arrange
        var command = new CreateStockMovementCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            InventoryMovementType.StockIn,
            10,
            new string('A', 101),
            null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Reference)
            .WithErrorMessage("Reference cannot exceed 100 characters.");
    }

    [Fact]
    public async Task Validate_WithReferenceExactly100Characters_ShouldPass()
    {
        // Arrange
        var command = new CreateStockMovementCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            InventoryMovementType.StockIn,
            10,
            new string('A', 100),
            null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Reference);
    }

    [Fact]
    public async Task Validate_WithNotesExceeding500Characters_ShouldFail()
    {
        // Arrange
        var command = new CreateStockMovementCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            InventoryMovementType.StockIn,
            10,
            null,
            new string('A', 501));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Notes)
            .WithErrorMessage("Notes cannot exceed 500 characters.");
    }

    [Fact]
    public async Task Validate_WithNotesExactly500Characters_ShouldPass()
    {
        // Arrange
        var command = new CreateStockMovementCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            InventoryMovementType.StockIn,
            10,
            null,
            new string('A', 500));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Notes);
    }

    [Fact]
    public async Task Validate_WithNullReferenceAndNotes_ShouldPass()
    {
        // Arrange
        var command = new CreateStockMovementCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            InventoryMovementType.Adjustment,
            -5,
            null,
            null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(InventoryMovementType.StockIn)]
    [InlineData(InventoryMovementType.StockOut)]
    [InlineData(InventoryMovementType.Adjustment)]
    [InlineData(InventoryMovementType.Return)]
    [InlineData(InventoryMovementType.Reservation)]
    [InlineData(InventoryMovementType.ReservationRelease)]
    public async Task Validate_WithValidEnumValues_ShouldPassMovementTypeValidation(
        InventoryMovementType movementType)
    {
        // Arrange
        var command = new CreateStockMovementCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            movementType,
            10,
            null,
            null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MovementType);
    }
}
