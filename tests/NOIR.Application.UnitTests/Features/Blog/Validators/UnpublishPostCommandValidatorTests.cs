namespace NOIR.Application.UnitTests.Features.Blog.Validators;

/// <summary>
/// Unit tests for UnpublishPostCommandValidator.
/// Tests all validation rules for unpublishing a blog post.
/// </summary>
public class UnpublishPostCommandValidatorTests
{
    private readonly UnpublishPostCommandValidator _validator = new();

    #region Id Validation

    [Fact]
    public async Task Validate_WhenIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new UnpublishPostCommand(Id: Guid.Empty);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorMessage("Post ID is required.");
    }

    [Fact]
    public async Task Validate_WhenIdIsValid_ShouldNotHaveError()
    {
        // Arrange
        var command = new UnpublishPostCommand(Id: Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Id);
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new UnpublishPostCommand(Id: Guid.NewGuid(), PostTitle: "My Blog Post");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenMinimalValidCommand_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new UnpublishPostCommand(Id: Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
