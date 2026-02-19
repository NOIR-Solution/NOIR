using NOIR.Application.Features.ProductAttributes.Commands.SetProductAttributeValue;

namespace NOIR.Application.UnitTests.Features.ProductAttributes.Validators;

/// <summary>
/// Unit tests for SetProductAttributeValueCommandValidator.
/// Tests all validation rules for setting a product attribute value.
/// </summary>
public class SetProductAttributeValueCommandValidatorTests
{
    private readonly SetProductAttributeValueCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new SetProductAttributeValueCommand(
            ProductId: Guid.NewGuid(),
            AttributeId: Guid.NewGuid(),
            VariantId: null,
            Value: "Red");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenCommandWithVariantIdIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new SetProductAttributeValueCommand(
            ProductId: Guid.NewGuid(),
            AttributeId: Guid.NewGuid(),
            VariantId: Guid.NewGuid(),
            Value: "Blue");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenProductIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new SetProductAttributeValueCommand(
            ProductId: Guid.Empty,
            AttributeId: Guid.NewGuid(),
            VariantId: null,
            Value: "Red");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProductId)
            .WithErrorMessage("Product ID is required.");
    }

    [Fact]
    public async Task Validate_WhenAttributeIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new SetProductAttributeValueCommand(
            ProductId: Guid.NewGuid(),
            AttributeId: Guid.Empty,
            VariantId: null,
            Value: "Red");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AttributeId)
            .WithErrorMessage("Attribute ID is required.");
    }

    [Fact]
    public async Task Validate_WhenBothIdsAreEmpty_ShouldHaveErrors()
    {
        // Arrange
        var command = new SetProductAttributeValueCommand(
            ProductId: Guid.Empty,
            AttributeId: Guid.Empty,
            VariantId: null,
            Value: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProductId);
        result.ShouldHaveValidationErrorFor(x => x.AttributeId);
    }
}
