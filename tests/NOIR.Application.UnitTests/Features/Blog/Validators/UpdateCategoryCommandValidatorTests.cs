using NOIR.Application.Features.Blog.Commands.UpdateCategory;

namespace NOIR.Application.UnitTests.Features.Blog.Validators;

/// <summary>
/// Unit tests for UpdateCategoryCommandValidator.
/// Tests all validation rules for updating a blog category.
/// </summary>
public class UpdateCategoryCommandValidatorTests
{
    private readonly UpdateCategoryCommandValidator _validator = new();

    #region Id Validation

    [Fact]
    public async Task Validate_WhenIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateCategoryCommand(
            Id: Guid.Empty,
            Name: "Valid Name",
            Slug: "valid-slug",
            Description: null,
            MetaTitle: null,
            MetaDescription: null,
            ImageUrl: null,
            SortOrder: 0,
            ParentId: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorMessage("Category ID is required.");
    }

    [Fact]
    public async Task Validate_WhenIdIsValid_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateCategoryCommand(
            Id: Guid.NewGuid(),
            Name: "Valid Name",
            Slug: "valid-slug",
            Description: null,
            MetaTitle: null,
            MetaDescription: null,
            ImageUrl: null,
            SortOrder: 0,
            ParentId: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Id);
    }

    #endregion

    #region Name Validation

    [Fact]
    public async Task Validate_WhenNameIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateCategoryCommand(
            Id: Guid.NewGuid(),
            Name: "",
            Slug: "valid-slug",
            Description: null,
            MetaTitle: null,
            MetaDescription: null,
            ImageUrl: null,
            SortOrder: 0,
            ParentId: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name is required.");
    }

    [Fact]
    public async Task Validate_WhenNameExceeds200Characters_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateCategoryCommand(
            Id: Guid.NewGuid(),
            Name: new string('A', 201),
            Slug: "valid-slug",
            Description: null,
            MetaTitle: null,
            MetaDescription: null,
            ImageUrl: null,
            SortOrder: 0,
            ParentId: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name cannot exceed 200 characters.");
    }

    #endregion

    #region Slug Validation

    [Fact]
    public async Task Validate_WhenSlugIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateCategoryCommand(
            Id: Guid.NewGuid(),
            Name: "Valid Name",
            Slug: "",
            Description: null,
            MetaTitle: null,
            MetaDescription: null,
            ImageUrl: null,
            SortOrder: 0,
            ParentId: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Slug)
            .WithErrorMessage("Slug is required.");
    }

    [Fact]
    public async Task Validate_WhenSlugExceeds200Characters_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateCategoryCommand(
            Id: Guid.NewGuid(),
            Name: "Valid Name",
            Slug: new string('a', 201),
            Description: null,
            MetaTitle: null,
            MetaDescription: null,
            ImageUrl: null,
            SortOrder: 0,
            ParentId: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Slug)
            .WithErrorMessage("Slug cannot exceed 200 characters.");
    }

    [Theory]
    [InlineData("Invalid Slug")]
    [InlineData("INVALID-SLUG")]
    public async Task Validate_WhenSlugHasInvalidFormat_ShouldHaveError(string slug)
    {
        // Arrange
        var command = new UpdateCategoryCommand(
            Id: Guid.NewGuid(),
            Name: "Valid Name",
            Slug: slug,
            Description: null,
            MetaTitle: null,
            MetaDescription: null,
            ImageUrl: null,
            SortOrder: 0,
            ParentId: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Slug)
            .WithErrorMessage("Slug must be lowercase and contain only letters, numbers, and hyphens.");
    }

    #endregion

    #region Description Validation

    [Fact]
    public async Task Validate_WhenDescriptionExceeds1000Characters_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateCategoryCommand(
            Id: Guid.NewGuid(),
            Name: "Valid Name",
            Slug: "valid-slug",
            Description: new string('A', 1001),
            MetaTitle: null,
            MetaDescription: null,
            ImageUrl: null,
            SortOrder: 0,
            ParentId: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description cannot exceed 1000 characters.");
    }

    #endregion

    #region MetaTitle Validation

    [Fact]
    public async Task Validate_WhenMetaTitleExceeds200Characters_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateCategoryCommand(
            Id: Guid.NewGuid(),
            Name: "Valid Name",
            Slug: "valid-slug",
            Description: null,
            MetaTitle: new string('A', 201),
            MetaDescription: null,
            ImageUrl: null,
            SortOrder: 0,
            ParentId: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MetaTitle)
            .WithErrorMessage("Meta title cannot exceed 200 characters.");
    }

    #endregion

    #region MetaDescription Validation

    [Fact]
    public async Task Validate_WhenMetaDescriptionExceeds500Characters_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateCategoryCommand(
            Id: Guid.NewGuid(),
            Name: "Valid Name",
            Slug: "valid-slug",
            Description: null,
            MetaTitle: null,
            MetaDescription: new string('A', 501),
            ImageUrl: null,
            SortOrder: 0,
            ParentId: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MetaDescription)
            .WithErrorMessage("Meta description cannot exceed 500 characters.");
    }

    #endregion

    #region ImageUrl Validation

    [Fact]
    public async Task Validate_WhenImageUrlExceeds2000Characters_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateCategoryCommand(
            Id: Guid.NewGuid(),
            Name: "Valid Name",
            Slug: "valid-slug",
            Description: null,
            MetaTitle: null,
            MetaDescription: null,
            ImageUrl: new string('a', 2001),
            SortOrder: 0,
            ParentId: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ImageUrl)
            .WithErrorMessage("Image URL cannot exceed 2000 characters.");
    }

    #endregion

    #region SortOrder Validation

    [Fact]
    public async Task Validate_WhenSortOrderIsNegative_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateCategoryCommand(
            Id: Guid.NewGuid(),
            Name: "Valid Name",
            Slug: "valid-slug",
            Description: null,
            MetaTitle: null,
            MetaDescription: null,
            ImageUrl: null,
            SortOrder: -1,
            ParentId: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SortOrder)
            .WithErrorMessage("Sort order must be a non-negative number.");
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new UpdateCategoryCommand(
            Id: Guid.NewGuid(),
            Name: "Technology",
            Slug: "technology",
            Description: "Articles about technology.",
            MetaTitle: "Technology | My Blog",
            MetaDescription: "Read our technology articles.",
            ImageUrl: "https://example.com/tech.jpg",
            SortOrder: 1,
            ParentId: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
