using NOIR.Application.Features.ProductAttributes.Commands.RemoveCategoryAttribute;

namespace NOIR.Application.UnitTests.Features.ProductAttributes.Validators;

/// <summary>
/// Unit tests for RemoveCategoryAttributeCommandValidator.
/// Tests all validation rules for removing an attribute from a category.
/// </summary>
public class RemoveCategoryAttributeCommandValidatorTests
{
    private readonly RemoveCategoryAttributeCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new RemoveCategoryAttributeCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenCategoryIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new RemoveCategoryAttributeCommand(Guid.Empty, Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CategoryId)
            .WithErrorMessage("Category ID is required.");
    }

    [Fact]
    public async Task Validate_WhenAttributeIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new RemoveCategoryAttributeCommand(Guid.NewGuid(), Guid.Empty);

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
        var command = new RemoveCategoryAttributeCommand(Guid.Empty, Guid.Empty);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CategoryId);
        result.ShouldHaveValidationErrorFor(x => x.AttributeId);
    }
}
