using NOIR.Application.Features.Reviews.Commands.BulkRejectReviews;

namespace NOIR.Application.UnitTests.Features.Reviews.Validators;

/// <summary>
/// Unit tests for BulkRejectReviewsCommandValidator.
/// Tests all validation rules for bulk rejecting reviews.
/// </summary>
public class BulkRejectReviewsCommandValidatorTests
{
    private readonly BulkRejectReviewsCommandValidator _validator = new();

    #region ReviewIds Validation

    [Fact]
    public async Task Validate_WhenReviewIdsIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new BulkRejectReviewsCommand(new List<Guid>(), "Spam");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ReviewIds)
            .WithErrorMessage("At least one review ID is required.");
    }

    [Fact]
    public async Task Validate_WhenReviewIdsContainsEmptyGuid_ShouldHaveError()
    {
        // Arrange
        var command = new BulkRejectReviewsCommand(new List<Guid> { Guid.Empty }, "Spam");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_WhenReviewIdsAreValid_ShouldNotHaveError()
    {
        // Arrange
        var command = new BulkRejectReviewsCommand(
            new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
            "Spam content");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ReviewIds);
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new BulkRejectReviewsCommand(
            new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
            "Inappropriate content");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenReasonIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new BulkRejectReviewsCommand(
            new List<Guid> { Guid.NewGuid() },
            null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
