using NOIR.Application.Features.Reviews.Commands.BulkApproveReviews;

namespace NOIR.Application.UnitTests.Features.Reviews.Validators;

/// <summary>
/// Unit tests for BulkApproveReviewsCommandValidator.
/// Tests all validation rules for bulk approving reviews.
/// </summary>
public class BulkApproveReviewsCommandValidatorTests
{
    private readonly BulkApproveReviewsCommandValidator _validator = new();

    #region ReviewIds Validation

    [Fact]
    public async Task Validate_WhenReviewIdsIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new BulkApproveReviewsCommand(new List<Guid>());

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
        var command = new BulkApproveReviewsCommand(new List<Guid> { Guid.Empty });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_WhenReviewIdsAreValid_ShouldNotHaveError()
    {
        // Arrange
        var command = new BulkApproveReviewsCommand(new List<Guid> { Guid.NewGuid(), Guid.NewGuid() });

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
        var command = new BulkApproveReviewsCommand(
            new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithSingleValidId_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new BulkApproveReviewsCommand(new List<Guid> { Guid.NewGuid() });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
