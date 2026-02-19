using NOIR.Application.Features.Products.Commands.CreateProductCategory;

namespace NOIR.Application.UnitTests.Features.Products.Commands.CreateProductCategory;

/// <summary>
/// Unit tests for CreateProductCategoryCommandValidator.
/// </summary>
public class CreateProductCategoryCommandValidatorTests
{
    private readonly CreateProductCategoryCommandValidator _validator;

    public CreateProductCategoryCommandValidatorTests()
    {
        _validator = new CreateProductCategoryCommandValidator();
    }

    private static CreateProductCategoryCommand CreateValidCommand() =>
        new(
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

    [Fact]
    public async Task Validate_WithMinimalValidCommand_ShouldPass()
    {
        // Arrange
        var command = new CreateProductCategoryCommand(
            Name: "Test",
            Slug: "test",
            Description: null,
            MetaTitle: null,
            MetaDescription: null,
            ImageUrl: null,
            SortOrder: 0,
            ParentId: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
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

    [Fact]
    public async Task Validate_WithNameAt200Characters_ShouldPass()
    {
        // Arrange
        var command = CreateValidCommand() with { Name = new string('A', 200) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
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
    [InlineData("-starts-with-hyphen")]
    [InlineData("ends-with-hyphen-")]
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
    [InlineData("home-and-garden")]
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

    [Fact]
    public async Task Validate_WithNullMetaTitle_ShouldPass()
    {
        // Arrange
        var command = CreateValidCommand() with { MetaTitle = null };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MetaTitle);
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

    [Fact]
    public async Task Validate_WithNullMetaDescription_ShouldPass()
    {
        // Arrange
        var command = CreateValidCommand() with { MetaDescription = null };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MetaDescription);
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

    [Fact]
    public async Task Validate_WithNullImageUrl_ShouldPass()
    {
        // Arrange
        var command = CreateValidCommand() with { ImageUrl = null };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ImageUrl);
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

    [Fact]
    public async Task Validate_WithZeroSortOrder_ShouldPass()
    {
        // Arrange
        var command = CreateValidCommand() with { SortOrder = 0 };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SortOrder);
    }
}
