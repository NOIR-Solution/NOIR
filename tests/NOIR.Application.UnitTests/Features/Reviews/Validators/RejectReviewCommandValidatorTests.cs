using NOIR.Application.Features.Reviews.Commands.RejectReview;

namespace NOIR.Application.UnitTests.Features.Reviews.Validators;

/// <summary>
/// Unit tests for RejectReviewCommandValidator.
/// Tests all validation rules for rejecting a review.
/// </summary>
public class RejectReviewCommandValidatorTests
{
    private readonly RejectReviewCommandValidator _validator = new();

    #region ReviewId Validation

    [Fact]
    public async Task Validate_WhenReviewIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new RejectReviewCommand(Guid.Empty, "Spam content");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ReviewId)
            .WithErrorMessage("Review ID is required.");
    }

    [Fact]
    public async Task Validate_WhenReviewIdIsValid_ShouldNotHaveError()
    {
        // Arrange
        var command = new RejectReviewCommand(Guid.NewGuid(), "Spam content");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ReviewId);
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new RejectReviewCommand(Guid.NewGuid(), "Inappropriate content");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenReasonIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new RejectReviewCommand(Guid.NewGuid(), null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
