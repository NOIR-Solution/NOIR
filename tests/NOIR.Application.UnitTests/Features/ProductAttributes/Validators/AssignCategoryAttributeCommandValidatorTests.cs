using NOIR.Application.Features.ProductAttributes.Commands.AssignCategoryAttribute;

namespace NOIR.Application.UnitTests.Features.ProductAttributes.Validators;

/// <summary>
/// Unit tests for AssignCategoryAttributeCommandValidator.
/// Tests all validation rules for assigning an attribute to a category.
/// </summary>
public class AssignCategoryAttributeCommandValidatorTests
{
    private readonly AssignCategoryAttributeCommandValidator _validator = new();

    private static AssignCategoryAttributeCommand CreateValidCommand() => new(
        CategoryId: Guid.NewGuid(),
        AttributeId: Guid.NewGuid(),
        IsRequired: false,
        SortOrder: 0);

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

    #region CategoryId Validation

    [Fact]
    public async Task Validate_WhenCategoryIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { CategoryId = Guid.Empty };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CategoryId)
            .WithErrorMessage("Category ID is required.");
    }

    #endregion

    #region AttributeId Validation

    [Fact]
    public async Task Validate_WhenAttributeIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { AttributeId = Guid.Empty };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AttributeId)
            .WithErrorMessage("Attribute ID is required.");
    }

    #endregion

    #region SortOrder Validation

    [Fact]
    public async Task Validate_WhenSortOrderIsNegative_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { SortOrder = -1 };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SortOrder)
            .WithErrorMessage("Sort order must be non-negative.");
    }

    [Fact]
    public async Task Validate_WhenSortOrderIsZero_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { SortOrder = 0 };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SortOrder);
    }

    #endregion
}
