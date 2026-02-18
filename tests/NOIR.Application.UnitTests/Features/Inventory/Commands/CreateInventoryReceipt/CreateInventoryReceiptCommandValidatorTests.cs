using NOIR.Application.Features.Inventory.Commands.CreateInventoryReceipt;
using NOIR.Application.Features.Inventory.DTOs;

namespace NOIR.Application.UnitTests.Features.Inventory.Commands.CreateInventoryReceipt;

/// <summary>
/// Unit tests for CreateInventoryReceiptCommandValidator.
/// </summary>
public class CreateInventoryReceiptCommandValidatorTests
{
    private readonly CreateInventoryReceiptCommandValidator _validator;

    public CreateInventoryReceiptCommandValidatorTests()
    {
        _validator = new CreateInventoryReceiptCommandValidator();
    }

    private static List<CreateInventoryReceiptItemDto> CreateValidItems(int count = 1)
    {
        var items = new List<CreateInventoryReceiptItemDto>();
        for (int i = 0; i < count; i++)
        {
            items.Add(new CreateInventoryReceiptItemDto(
                Guid.NewGuid(),
                Guid.NewGuid(),
                $"Product {i + 1}",
                $"Variant {i + 1}",
                $"SKU-{i + 1:D3}",
                10,
                25.00m));
        }
        return items;
    }

    [Fact]
    public async Task Validate_WithValidCommand_ShouldPass()
    {
        // Arrange
        var command = new CreateInventoryReceiptCommand(
            InventoryReceiptType.StockIn,
            "Test notes",
            CreateValidItems());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithEmptyItems_ShouldFail()
    {
        // Arrange
        var command = new CreateInventoryReceiptCommand(
            InventoryReceiptType.StockIn,
            null,
            new List<CreateInventoryReceiptItemDto>());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Items)
            .WithErrorMessage("At least one item is required.");
    }

    [Fact]
    public async Task Validate_WithNotesExceeding1000Characters_ShouldFail()
    {
        // Arrange
        var command = new CreateInventoryReceiptCommand(
            InventoryReceiptType.StockIn,
            new string('A', 1001),
            CreateValidItems());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Notes)
            .WithErrorMessage("Notes cannot exceed 1000 characters.");
    }

    [Fact]
    public async Task Validate_WithNullNotes_ShouldPass()
    {
        // Arrange
        var command = new CreateInventoryReceiptCommand(
            InventoryReceiptType.StockIn,
            null,
            CreateValidItems());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithItemHavingEmptyProductVariantId_ShouldFail()
    {
        // Arrange
        var items = new List<CreateInventoryReceiptItemDto>
        {
            new(Guid.Empty, Guid.NewGuid(), "Product", "Variant", "SKU", 10, 25.00m)
        };
        var command = new CreateInventoryReceiptCommand(
            InventoryReceiptType.StockIn,
            null,
            items);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_WithItemHavingEmptyProductId_ShouldFail()
    {
        // Arrange
        var items = new List<CreateInventoryReceiptItemDto>
        {
            new(Guid.NewGuid(), Guid.Empty, "Product", "Variant", "SKU", 10, 25.00m)
        };
        var command = new CreateInventoryReceiptCommand(
            InventoryReceiptType.StockIn,
            null,
            items);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_WithItemHavingEmptyProductName_ShouldFail()
    {
        // Arrange
        var items = new List<CreateInventoryReceiptItemDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "", "Variant", "SKU", 10, 25.00m)
        };
        var command = new CreateInventoryReceiptCommand(
            InventoryReceiptType.StockIn,
            null,
            items);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_WithItemHavingZeroQuantity_ShouldFail()
    {
        // Arrange
        var items = new List<CreateInventoryReceiptItemDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "Product", "Variant", "SKU", 0, 25.00m)
        };
        var command = new CreateInventoryReceiptCommand(
            InventoryReceiptType.StockIn,
            null,
            items);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_WithItemHavingNegativeUnitCost_ShouldFail()
    {
        // Arrange
        var items = new List<CreateInventoryReceiptItemDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "Product", "Variant", "SKU", 10, -5.00m)
        };
        var command = new CreateInventoryReceiptCommand(
            InventoryReceiptType.StockIn,
            null,
            items);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_WithItemHavingZeroUnitCost_ShouldPass()
    {
        // Arrange
        var items = new List<CreateInventoryReceiptItemDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "Product", "Variant", "SKU", 10, 0m)
        };
        var command = new CreateInventoryReceiptCommand(
            InventoryReceiptType.StockIn,
            null,
            items);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
