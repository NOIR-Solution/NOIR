using NOIR.Application.Features.Blog.Commands.CreateTag;

namespace NOIR.Application.UnitTests.Features.Blog.Validators;

/// <summary>
/// Unit tests for CreateTagCommandValidator.
/// Tests all validation rules for creating a blog tag.
/// </summary>
public class CreateTagCommandValidatorTests
{
    private readonly CreateTagCommandValidator _validator = new();

    #region Name Validation

    [Fact]
    public async Task Validate_WhenNameIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new CreateTagCommand(
            Name: "",
            Slug: "valid-slug",
            Description: null,
            Color: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name is required.");
    }

    [Fact]
    public async Task Validate_WhenNameExceeds100Characters_ShouldHaveError()
    {
        // Arrange
        var command = new CreateTagCommand(
            Name: new string('A', 101),
            Slug: "valid-slug",
            Description: null,
            Color: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name cannot exceed 100 characters.");
    }

    [Fact]
    public async Task Validate_WhenNameIs100Characters_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreateTagCommand(
            Name: new string('A', 100),
            Slug: "valid-slug",
            Description: null,
            Color: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    #endregion

    #region Slug Validation

    [Fact]
    public async Task Validate_WhenSlugIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new CreateTagCommand(
            Name: "Valid Name",
            Slug: "",
            Description: null,
            Color: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Slug)
            .WithErrorMessage("Slug is required.");
    }

    [Fact]
    public async Task Validate_WhenSlugExceeds100Characters_ShouldHaveError()
    {
        // Arrange
        var command = new CreateTagCommand(
            Name: "Valid Name",
            Slug: new string('a', 101),
            Description: null,
            Color: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Slug)
            .WithErrorMessage("Slug cannot exceed 100 characters.");
    }

    [Theory]
    [InlineData("Invalid Slug")]
    [InlineData("INVALID-SLUG")]
    [InlineData("-invalid-slug")]
    [InlineData("invalid-slug-")]
    public async Task Validate_WhenSlugHasInvalidFormat_ShouldHaveError(string slug)
    {
        // Arrange
        var command = new CreateTagCommand(
            Name: "Valid Name",
            Slug: slug,
            Description: null,
            Color: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Slug)
            .WithErrorMessage("Slug must be lowercase and contain only letters, numbers, and hyphens.");
    }

    [Theory]
    [InlineData("valid-slug")]
    [InlineData("csharp")]
    [InlineData("dotnet-core")]
    public async Task Validate_WhenSlugHasValidFormat_ShouldNotHaveError(string slug)
    {
        // Arrange
        var command = new CreateTagCommand(
            Name: "Valid Name",
            Slug: slug,
            Description: null,
            Color: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Slug);
    }

    #endregion

    #region Description Validation

    [Fact]
    public async Task Validate_WhenDescriptionExceeds500Characters_ShouldHaveError()
    {
        // Arrange
        var command = new CreateTagCommand(
            Name: "Valid Name",
            Slug: "valid-slug",
            Description: new string('A', 501),
            Color: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description cannot exceed 500 characters.");
    }

    [Fact]
    public async Task Validate_WhenDescriptionIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreateTagCommand(
            Name: "Valid Name",
            Slug: "valid-slug",
            Description: null,
            Color: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    #endregion

    #region Color Validation

    [Fact]
    public async Task Validate_WhenColorExceeds20Characters_ShouldHaveError()
    {
        // Arrange
        var command = new CreateTagCommand(
            Name: "Valid Name",
            Slug: "valid-slug",
            Description: null,
            Color: new string('A', 21));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Color)
            .WithErrorMessage("Color cannot exceed 20 characters.");
    }

    [Theory]
    [InlineData("red")]
    [InlineData("#3B82F")]
    [InlineData("#3B82F6G")]
    [InlineData("3B82F6")]
    public async Task Validate_WhenColorHasInvalidFormat_ShouldHaveError(string color)
    {
        // Arrange
        var command = new CreateTagCommand(
            Name: "Valid Name",
            Slug: "valid-slug",
            Description: null,
            Color: color);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Color)
            .WithErrorMessage("Color must be a valid hex color code (e.g., #3B82F6).");
    }

    [Theory]
    [InlineData("#3B82F6")]
    [InlineData("#ffffff")]
    [InlineData("#000000")]
    [InlineData("#ABCDEF")]
    public async Task Validate_WhenColorHasValidFormat_ShouldNotHaveError(string color)
    {
        // Arrange
        var command = new CreateTagCommand(
            Name: "Valid Name",
            Slug: "valid-slug",
            Description: null,
            Color: color);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Color);
    }

    [Fact]
    public async Task Validate_WhenColorIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreateTagCommand(
            Name: "Valid Name",
            Slug: "valid-slug",
            Description: null,
            Color: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Color);
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new CreateTagCommand(
            Name: "C#",
            Slug: "csharp",
            Description: "Posts about C# programming.",
            Color: "#3B82F6");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenMinimalValidCommand_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new CreateTagCommand(
            Name: "C#",
            Slug: "csharp",
            Description: null,
            Color: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
