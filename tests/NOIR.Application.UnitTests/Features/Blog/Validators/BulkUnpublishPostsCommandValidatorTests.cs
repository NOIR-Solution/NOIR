using NOIR.Application.Features.Blog.Commands.BulkUnpublishPosts;

namespace NOIR.Application.UnitTests.Features.Blog.Validators;

/// <summary>
/// Unit tests for BulkUnpublishPostsCommandValidator.
/// Tests all validation rules for bulk unpublishing blog posts.
/// </summary>
public class BulkUnpublishPostsCommandValidatorTests
{
    private readonly BulkUnpublishPostsCommandValidator _validator = new();

    #region PostIds Validation

    [Fact]
    public async Task Validate_WhenPostIdsIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new BulkUnpublishPostsCommand(new List<Guid>());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PostIds)
            .WithErrorMessage("At least one post ID is required.");
    }

    [Fact]
    public async Task Validate_WhenPostIdsExceedsMaximum_ShouldHaveError()
    {
        // Arrange
        var postIds = Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()).ToList();
        var command = new BulkUnpublishPostsCommand(postIds);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PostIds.Count)
            .WithErrorMessage("Maximum 100 posts per batch.");
    }

    [Fact]
    public async Task Validate_WhenPostIdContainsEmptyGuid_ShouldHaveError()
    {
        // Arrange
        var command = new BulkUnpublishPostsCommand(new List<Guid> { Guid.NewGuid(), Guid.Empty });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.ShouldBe(false);
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenSingleValidId_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new BulkUnpublishPostsCommand(new List<Guid> { Guid.NewGuid() });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenExactly100Ids_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var postIds = Enumerable.Range(0, 100).Select(_ => Guid.NewGuid()).ToList();
        var command = new BulkUnpublishPostsCommand(postIds);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenMultipleValidIds_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new BulkUnpublishPostsCommand(
            new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
