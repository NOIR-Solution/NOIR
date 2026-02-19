using NOIR.Application.Features.Products.Commands.ReorderProductCategories;

namespace NOIR.Application.UnitTests.Features.Products.Commands.ReorderProductCategories;

/// <summary>
/// Unit tests for ReorderProductCategoriesCommandValidator.
/// </summary>
public class ReorderProductCategoriesCommandValidatorTests
{
    private readonly ReorderProductCategoriesCommandValidator _validator;

    public ReorderProductCategoriesCommandValidatorTests()
    {
        _validator = new ReorderProductCategoriesCommandValidator();
    }

    [Fact]
    public async Task Validate_WithValidCommand_ShouldPass()
    {
        // Arrange
        var command = new ReorderProductCategoriesCommand(
            new List<CategorySortOrderItem>
            {
                new(Guid.NewGuid(), null, 0),
                new(Guid.NewGuid(), null, 1)
            });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithSingleItem_ShouldPass()
    {
        // Arrange
        var command = new ReorderProductCategoriesCommand(
            new List<CategorySortOrderItem>
            {
                new(Guid.NewGuid(), Guid.NewGuid(), 0)
            });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ===== Items NotEmpty Validation =====

    [Fact]
    public async Task Validate_WithEmptyItems_ShouldFail()
    {
        // Arrange
        var command = new ReorderProductCategoriesCommand(new List<CategorySortOrderItem>());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Items)
            .WithErrorMessage("At least one category must be provided for reordering.");
    }

    // ===== Item Child Validation =====

    [Fact]
    public async Task Validate_WithItemHavingEmptyCategoryId_ShouldFail()
    {
        // Arrange
        var command = new ReorderProductCategoriesCommand(
            new List<CategorySortOrderItem>
            {
                new(Guid.Empty, null, 0)
            });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_WithItemHavingNegativeSortOrder_ShouldFail()
    {
        // Arrange
        var command = new ReorderProductCategoriesCommand(
            new List<CategorySortOrderItem>
            {
                new(Guid.NewGuid(), null, -1)
            });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_WithMultipleValidItems_ShouldPass()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var command = new ReorderProductCategoriesCommand(
            new List<CategorySortOrderItem>
            {
                new(Guid.NewGuid(), null, 0),
                new(Guid.NewGuid(), parentId, 1),
                new(Guid.NewGuid(), parentId, 2)
            });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
