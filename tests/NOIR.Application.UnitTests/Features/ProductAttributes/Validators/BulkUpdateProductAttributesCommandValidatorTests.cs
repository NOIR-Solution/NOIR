using NOIR.Application.Features.ProductAttributes.Commands.BulkUpdateProductAttributes;
using NOIR.Application.Features.ProductAttributes.DTOs;

namespace NOIR.Application.UnitTests.Features.ProductAttributes.Validators;

/// <summary>
/// Unit tests for BulkUpdateProductAttributesCommandValidator.
/// Tests all validation rules for bulk updating product attributes.
/// </summary>
public class BulkUpdateProductAttributesCommandValidatorTests
{
    private readonly BulkUpdateProductAttributesCommandValidator _validator = new();

    private static BulkUpdateProductAttributesCommand CreateValidCommand() => new(
        ProductId: Guid.NewGuid(),
        VariantId: null,
        Values: new List<AttributeValueItem>
        {
            new(Guid.NewGuid(), "Red"),
            new(Guid.NewGuid(), 42)
        });

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
    public async Task Validate_WhenCommandWithVariantIdIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = CreateValidCommand() with { VariantId = Guid.NewGuid() };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenValuesIsEmpty_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = CreateValidCommand() with { Values = new List<AttributeValueItem>() };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region ProductId Validation

    [Fact]
    public async Task Validate_WhenProductIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { ProductId = Guid.Empty };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProductId)
            .WithErrorMessage("Product ID is required.");
    }

    #endregion

    #region Values Validation

    [Fact]
    public async Task Validate_WhenValuesIsNull_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Values = null! };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Values)
            .WithErrorMessage("Values collection is required.");
    }

    [Fact]
    public async Task Validate_WhenValueItemHasEmptyAttributeId_ShouldHaveError()
    {
        // Arrange
        var command = new BulkUpdateProductAttributesCommand(
            ProductId: Guid.NewGuid(),
            VariantId: null,
            Values: new List<AttributeValueItem>
            {
                new(Guid.Empty, "Red")
            });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_WhenAllValueItemsHaveValidAttributeIds_ShouldNotHaveError()
    {
        // Arrange
        var command = new BulkUpdateProductAttributesCommand(
            ProductId: Guid.NewGuid(),
            VariantId: null,
            Values: new List<AttributeValueItem>
            {
                new(Guid.NewGuid(), "Red"),
                new(Guid.NewGuid(), "Blue"),
                new(Guid.NewGuid(), 42)
            });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
