using NOIR.Application.Features.Blog.Commands.ReorderCategories;

namespace NOIR.Application.UnitTests.Features.Blog.Validators;

/// <summary>
/// Unit tests for ReorderBlogCategoriesCommandValidator.
/// Tests all validation rules for reordering blog categories.
/// </summary>
public class ReorderBlogCategoriesCommandValidatorTests
{
    private readonly ReorderBlogCategoriesCommandValidator _validator = new();

    #region Items Validation

    [Fact]
    public async Task Validate_WhenItemsIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new ReorderBlogCategoriesCommand(
            Items: new List<BlogCategorySortOrderItem>());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Items)
            .WithErrorMessage("At least one category must be provided for reordering.");
    }

    [Fact]
    public async Task Validate_WhenItemsIsNull_ShouldHaveError()
    {
        // Arrange
        var command = new ReorderBlogCategoriesCommand(
            Items: null!);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Items);
    }

    [Fact]
    public async Task Validate_WhenItemsHasOneEntry_ShouldNotHaveError()
    {
        // Arrange
        var command = new ReorderBlogCategoriesCommand(
            Items: new List<BlogCategorySortOrderItem>
            {
                new(CategoryId: Guid.NewGuid(), ParentId: null, SortOrder: 0)
            });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Items);
    }

    #endregion

    #region Items.CategoryId Validation

    [Fact]
    public async Task Validate_WhenCategoryIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new ReorderBlogCategoriesCommand(
            Items: new List<BlogCategorySortOrderItem>
            {
                new(CategoryId: Guid.Empty, ParentId: null, SortOrder: 0)
            });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Category ID is required.");
    }

    [Fact]
    public async Task Validate_WhenCategoryIdIsValid_ShouldNotHaveError()
    {
        // Arrange
        var command = new ReorderBlogCategoriesCommand(
            Items: new List<BlogCategorySortOrderItem>
            {
                new(CategoryId: Guid.NewGuid(), ParentId: null, SortOrder: 0)
            });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Items.SortOrder Validation

    [Fact]
    public async Task Validate_WhenSortOrderIsNegative_ShouldHaveError()
    {
        // Arrange
        var command = new ReorderBlogCategoriesCommand(
            Items: new List<BlogCategorySortOrderItem>
            {
                new(CategoryId: Guid.NewGuid(), ParentId: null, SortOrder: -1)
            });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Sort order must be a non-negative number.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    public async Task Validate_WhenSortOrderIsNonNegative_ShouldNotHaveError(int sortOrder)
    {
        // Arrange
        var command = new ReorderBlogCategoriesCommand(
            Items: new List<BlogCategorySortOrderItem>
            {
                new(CategoryId: Guid.NewGuid(), ParentId: null, SortOrder: sortOrder)
            });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new ReorderBlogCategoriesCommand(
            Items: new List<BlogCategorySortOrderItem>
            {
                new(CategoryId: Guid.NewGuid(), ParentId: null, SortOrder: 0),
                new(CategoryId: Guid.NewGuid(), ParentId: null, SortOrder: 1),
                new(CategoryId: Guid.NewGuid(), ParentId: Guid.NewGuid(), SortOrder: 2)
            });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenMultipleItemsHaveInvalidData_ShouldHaveMultipleErrors()
    {
        // Arrange
        var command = new ReorderBlogCategoriesCommand(
            Items: new List<BlogCategorySortOrderItem>
            {
                new(CategoryId: Guid.Empty, ParentId: null, SortOrder: -1),
                new(CategoryId: Guid.Empty, ParentId: null, SortOrder: -2)
            });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Category ID is required.");
        result.Errors.Should().Contain(e => e.ErrorMessage == "Sort order must be a non-negative number.");
    }

    #endregion
}
