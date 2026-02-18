using NOIR.Application.Features.Reviews.Commands.CreateReview;

namespace NOIR.Application.UnitTests.Features.Reviews.Validators;

/// <summary>
/// Unit tests for CreateReviewCommandValidator.
/// Tests all validation rules for creating a review.
/// </summary>
public class CreateReviewCommandValidatorTests
{
    private readonly CreateReviewCommandValidator _validator = new();

    private static CreateReviewCommand CreateValidCommand(
        Guid? productId = null,
        int rating = 4,
        string? title = "Great product",
        string content = "This product is really excellent and works well.",
        Guid? orderId = null,
        List<string>? mediaUrls = null)
    {
        return new CreateReviewCommand(
            productId ?? Guid.NewGuid(),
            rating,
            title,
            content,
            orderId,
            mediaUrls);
    }

    #region ProductId Validation

    [Fact]
    public async Task Validate_WhenProductIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(productId: Guid.Empty);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProductId)
            .WithErrorMessage("Product ID is required.");
    }

    [Fact]
    public async Task Validate_WhenProductIdIsValid_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand(productId: Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProductId);
    }

    #endregion

    #region Rating Validation

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(6)]
    [InlineData(10)]
    public async Task Validate_WhenRatingIsOutOfRange_ShouldHaveError(int rating)
    {
        // Arrange
        var command = CreateValidCommand(rating: rating);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Rating)
            .WithErrorMessage("Rating must be between 1 and 5.");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public async Task Validate_WhenRatingIsInRange_ShouldNotHaveError(int rating)
    {
        // Arrange
        var command = CreateValidCommand(rating: rating);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Rating);
    }

    #endregion

    #region Title Validation

    [Fact]
    public async Task Validate_WhenTitleExceeds200Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(title: new string('A', 201));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage("Title cannot exceed 200 characters.");
    }

    [Fact]
    public async Task Validate_WhenTitleIs200Characters_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand(title: new string('A', 200));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public async Task Validate_WhenTitleIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand(title: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Title);
    }

    #endregion

    #region Content Validation

    [Fact]
    public async Task Validate_WhenContentIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(content: "");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Content)
            .WithErrorMessage("Review content is required.");
    }

    [Fact]
    public async Task Validate_WhenContentIsTooShort_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(content: "Short");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Content)
            .WithErrorMessage("Review content must be at least 10 characters.");
    }

    [Fact]
    public async Task Validate_WhenContentExceeds2000Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(content: new string('A', 2001));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Content)
            .WithErrorMessage("Review content cannot exceed 2000 characters.");
    }

    [Fact]
    public async Task Validate_WhenContentIsExactlyMinLength_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand(content: new string('A', 10));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Content);
    }

    [Fact]
    public async Task Validate_WhenContentIsExactlyMaxLength_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand(content: new string('A', 2000));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Content);
    }

    #endregion

    #region MediaUrls Validation

    [Fact]
    public async Task Validate_WhenMediaUrlsExceed5Items_ShouldHaveError()
    {
        // Arrange
        var mediaUrls = Enumerable.Range(1, 6)
            .Select(i => $"https://cdn.test.com/image{i}.jpg")
            .ToList();
        var command = CreateValidCommand(mediaUrls: mediaUrls);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MediaUrls)
            .WithErrorMessage("Maximum 5 media items allowed.");
    }

    [Fact]
    public async Task Validate_WhenMediaUrlsHas5Items_ShouldNotHaveError()
    {
        // Arrange
        var mediaUrls = Enumerable.Range(1, 5)
            .Select(i => $"https://cdn.test.com/image{i}.jpg")
            .ToList();
        var command = CreateValidCommand(mediaUrls: mediaUrls);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MediaUrls);
    }

    [Fact]
    public async Task Validate_WhenMediaUrlsIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand(mediaUrls: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MediaUrls);
    }

    [Fact]
    public async Task Validate_WhenMediaUrlIsEmpty_ShouldHaveError()
    {
        // Arrange
        var mediaUrls = new List<string> { "" };
        var command = CreateValidCommand(mediaUrls: mediaUrls);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_WhenMediaUrlExceeds500Characters_ShouldHaveError()
    {
        // Arrange
        var mediaUrls = new List<string> { new string('a', 501) };
        var command = CreateValidCommand(mediaUrls: mediaUrls);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new CreateReviewCommand(
            Guid.NewGuid(),
            5,
            "Excellent product!",
            "This product exceeded my expectations and I would highly recommend it.",
            Guid.NewGuid(),
            new List<string> { "https://cdn.test.com/image1.jpg" });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenMinimalValidCommand_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new CreateReviewCommand(
            Guid.NewGuid(),
            3,
            null,
            "This is a valid review content.",
            null,
            null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
