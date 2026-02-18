using NOIR.Application.Features.Reviews.Commands.AddAdminResponse;

namespace NOIR.Application.UnitTests.Features.Reviews.Validators;

/// <summary>
/// Unit tests for AddAdminResponseCommandValidator.
/// Tests all validation rules for adding an admin response.
/// </summary>
public class AddAdminResponseCommandValidatorTests
{
    private readonly AddAdminResponseCommandValidator _validator = new();

    #region ReviewId Validation

    [Fact]
    public async Task Validate_WhenReviewIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new AddAdminResponseCommand(Guid.Empty, "Thank you for your feedback!");

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
        var command = new AddAdminResponseCommand(Guid.NewGuid(), "Thank you for your feedback!");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ReviewId);
    }

    #endregion

    #region Response Validation

    [Fact]
    public async Task Validate_WhenResponseIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new AddAdminResponseCommand(Guid.NewGuid(), "");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Response)
            .WithErrorMessage("Response is required.");
    }

    [Fact]
    public async Task Validate_WhenResponseExceeds2000Characters_ShouldHaveError()
    {
        // Arrange
        var command = new AddAdminResponseCommand(Guid.NewGuid(), new string('A', 2001));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Response)
            .WithErrorMessage("Response cannot exceed 2000 characters.");
    }

    [Fact]
    public async Task Validate_WhenResponseIs2000Characters_ShouldNotHaveError()
    {
        // Arrange
        var command = new AddAdminResponseCommand(Guid.NewGuid(), new string('A', 2000));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Response);
    }

    [Fact]
    public async Task Validate_WhenResponseIsValid_ShouldNotHaveError()
    {
        // Arrange
        var command = new AddAdminResponseCommand(Guid.NewGuid(), "Thank you for your feedback!");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Response);
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new AddAdminResponseCommand(
            Guid.NewGuid(),
            "Thank you for your review! We appreciate your feedback.");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
