using NOIR.Application.Features.Reviews.Commands.VoteReview;

namespace NOIR.Application.UnitTests.Features.Reviews.Validators;

/// <summary>
/// Unit tests for VoteReviewCommandValidator.
/// Tests all validation rules for voting on a review.
/// </summary>
public class VoteReviewCommandValidatorTests
{
    private readonly VoteReviewCommandValidator _validator = new();

    #region ReviewId Validation

    [Fact]
    public async Task Validate_WhenReviewIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new VoteReviewCommand(Guid.Empty, IsHelpful: true);

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
        var command = new VoteReviewCommand(Guid.NewGuid(), IsHelpful: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ReviewId);
    }

    #endregion

    #region Valid Command Tests

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors(bool isHelpful)
    {
        // Arrange
        var command = new VoteReviewCommand(Guid.NewGuid(), isHelpful);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
