using NOIR.Application.Features.ProductAttributes.Commands.RemoveProductAttributeValue;

namespace NOIR.Application.UnitTests.Features.ProductAttributes.Validators;

/// <summary>
/// Unit tests for RemoveProductAttributeValueCommandValidator.
/// Tests all validation rules for removing a product attribute value.
/// </summary>
public class RemoveProductAttributeValueCommandValidatorTests
{
    private readonly RemoveProductAttributeValueCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new RemoveProductAttributeValueCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenAttributeIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new RemoveProductAttributeValueCommand(Guid.Empty, Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AttributeId)
            .WithErrorMessage("Attribute ID is required.");
    }

    [Fact]
    public async Task Validate_WhenValueIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new RemoveProductAttributeValueCommand(Guid.NewGuid(), Guid.Empty);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ValueId)
            .WithErrorMessage("Value ID is required.");
    }

    [Fact]
    public async Task Validate_WhenBothIdsAreEmpty_ShouldHaveErrors()
    {
        // Arrange
        var command = new RemoveProductAttributeValueCommand(Guid.Empty, Guid.Empty);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AttributeId);
        result.ShouldHaveValidationErrorFor(x => x.ValueId);
    }
}
