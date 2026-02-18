using NOIR.Application.Features.Reviews.Commands.ApproveReview;

namespace NOIR.Application.UnitTests.Features.Reviews.Validators;

/// <summary>
/// Unit tests for ApproveReviewCommandValidator.
/// Tests all validation rules for approving a review.
/// </summary>
public class ApproveReviewCommandValidatorTests
{
    private readonly ApproveReviewCommandValidator _validator = new();

    #region ReviewId Validation

    [Fact]
    public async Task Validate_WhenReviewIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new ApproveReviewCommand(Guid.Empty);

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
        var command = new ApproveReviewCommand(Guid.NewGuid());

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
        var command = new ApproveReviewCommand(Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
