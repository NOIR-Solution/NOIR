using NOIR.Application.Features.Blog.Commands.PublishPost;

namespace NOIR.Application.UnitTests.Features.Blog.Validators;

/// <summary>
/// Unit tests for PublishPostCommandValidator.
/// Tests all validation rules for publishing a blog post.
/// </summary>
public class PublishPostCommandValidatorTests
{
    private readonly PublishPostCommandValidator _validator = new();

    #region Id Validation

    [Fact]
    public async Task Validate_WhenIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new PublishPostCommand(Id: Guid.Empty);

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
        var command = new PublishPostCommand(Id: Guid.NewGuid());

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
        var command = new PublishPostCommand(
            Id: Guid.NewGuid(),
            ScheduledPublishAt: DateTimeOffset.UtcNow.AddDays(1),
            PostTitle: "My Blog Post");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenMinimalValidCommand_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new PublishPostCommand(Id: Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenPublishImmediately_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new PublishPostCommand(
            Id: Guid.NewGuid(),
            ScheduledPublishAt: null,
            PostTitle: "My Blog Post");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
