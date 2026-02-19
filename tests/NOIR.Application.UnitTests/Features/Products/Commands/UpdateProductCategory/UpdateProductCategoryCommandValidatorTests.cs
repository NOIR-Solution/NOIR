using NOIR.Application.Features.Products.Commands.UpdateProductCategory;

namespace NOIR.Application.UnitTests.Features.Products.Commands.UpdateProductCategory;

/// <summary>
/// Unit tests for UpdateProductCategoryCommandValidator.
/// </summary>
public class UpdateProductCategoryCommandValidatorTests
{
    private readonly UpdateProductCategoryCommandValidator _validator;

    public UpdateProductCategoryCommandValidatorTests()
    {
        _validator = new UpdateProductCategoryCommandValidator();
    }

    private static UpdateProductCategoryCommand CreateValidCommand() =>
        new(
            Id: Guid.NewGuid(),
            Name: "Electronics",
            Slug: "electronics",
            Description: "Electronic products",
            MetaTitle: "Electronics Category",
            MetaDescription: "Browse electronic products",
            ImageUrl: "https://example.com/electronics.jpg",
            SortOrder: 0,
            ParentId: null);

    [Fact]
    public async Task Validate_WithValidCommand_ShouldPass()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ===== Id Validation =====

    [Fact]
    public async Task Validate_WithEmptyId_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { Id = Guid.Empty };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorMessage("Category ID is required.");
    }

    // ===== Name Validation =====

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_WithEmptyName_ShouldFail(string? name)
    {
        // Arrange
        var command = CreateValidCommand() with { Name = name! };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Validate_WithNameExceeding200Characters_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { Name = new string('A', 201) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name cannot exceed 200 characters.");
    }

    // ===== Slug Validation =====

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_WithEmptySlug_ShouldFail(string? slug)
    {
        // Arrange
        var command = CreateValidCommand() with { Slug = slug! };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Slug);
    }

    [Fact]
    public async Task Validate_WithSlugExceeding200Characters_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { Slug = new string('a', 201) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Slug);
    }

    [Theory]
    [InlineData("UPPERCASE")]
    [InlineData("has spaces")]
    [InlineData("has_underscore")]
    public async Task Validate_WithInvalidSlugFormat_ShouldFail(string slug)
    {
        // Arrange
        var command = CreateValidCommand() with { Slug = slug };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Slug)
            .WithErrorMessage("Slug must be lowercase and contain only letters, numbers, and hyphens.");
    }

    [Theory]
    [InlineData("valid-slug")]
    [InlineData("electronics")]
    public async Task Validate_WithValidSlugFormat_ShouldPass(string slug)
    {
        // Arrange
        var command = CreateValidCommand() with { Slug = slug };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Slug);
    }

    // ===== Description Validation =====

    [Fact]
    public async Task Validate_WithDescriptionExceeding2000Characters_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { Description = new string('A', 2001) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description cannot exceed 2000 characters.");
    }

    [Fact]
    public async Task Validate_WithNullDescription_ShouldPass()
    {
        // Arrange
        var command = CreateValidCommand() with { Description = null };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    // ===== MetaTitle Validation =====

    [Fact]
    public async Task Validate_WithMetaTitleExceeding200Characters_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { MetaTitle = new string('A', 201) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MetaTitle)
            .WithErrorMessage("Meta title cannot exceed 200 characters.");
    }

    // ===== MetaDescription Validation =====

    [Fact]
    public async Task Validate_WithMetaDescriptionExceeding500Characters_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { MetaDescription = new string('A', 501) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MetaDescription)
            .WithErrorMessage("Meta description cannot exceed 500 characters.");
    }

    // ===== ImageUrl Validation =====

    [Fact]
    public async Task Validate_WithImageUrlExceeding2000Characters_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { ImageUrl = new string('A', 2001) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ImageUrl)
            .WithErrorMessage("Image URL cannot exceed 2000 characters.");
    }

    // ===== SortOrder Validation =====

    [Fact]
    public async Task Validate_WithNegativeSortOrder_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { SortOrder = -1 };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SortOrder)
            .WithErrorMessage("Sort order must be a non-negative number.");
    }
}
