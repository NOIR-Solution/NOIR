namespace NOIR.Application.UnitTests.Features.Auth.Validators;

/// <summary>
/// Unit tests for DeleteAvatarCommandValidator.
/// </summary>
public class DeleteAvatarCommandValidatorTests
{
    private readonly DeleteAvatarCommandValidator _validator;

    public DeleteAvatarCommandValidatorTests()
    {
        _validator = new DeleteAvatarCommandValidator();
    }

    [Fact]
    public async Task Validate_WhenUserIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new DeleteAvatarCommand { UserId = string.Empty };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage("User ID is required.");
    }

    [Fact]
    public async Task Validate_WhenUserIdIsNull_ShouldHaveError()
    {
        // Arrange
        var command = new DeleteAvatarCommand { UserId = null };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public async Task Validate_WhenUserIdIsWhitespace_ShouldHaveError()
    {
        // Arrange
        var command = new DeleteAvatarCommand { UserId = "   " };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public async Task Validate_WhenUserIdIsValid_ShouldNotHaveError()
    {
        // Arrange
        var command = new DeleteAvatarCommand { UserId = "user-123" };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
