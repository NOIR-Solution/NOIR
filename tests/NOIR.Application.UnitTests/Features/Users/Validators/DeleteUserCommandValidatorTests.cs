namespace NOIR.Application.UnitTests.Features.Users.Validators;

/// <summary>
/// Unit tests for DeleteUserCommandValidator.
/// Tests all validation rules for deleting a user.
/// </summary>
public class DeleteUserCommandValidatorTests
{
    private readonly DeleteUserCommandValidator _validator;

    public DeleteUserCommandValidatorTests()
    {
        _validator = new DeleteUserCommandValidator(CreateLocalizationMock());
    }

    private static ILocalizationService CreateLocalizationMock()
    {
        var mock = new Mock<ILocalizationService>();
        mock.Setup(x => x["validation.userId.required"]).Returns("User ID is required");
        return mock.Object;
    }

    #region UserId Validation

    [Fact]
    public async Task Validate_WhenUserIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new DeleteUserCommand("");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage("User ID is required");
    }

    [Fact]
    public async Task Validate_WhenUserIdIsNull_ShouldHaveError()
    {
        // Arrange
        var command = new DeleteUserCommand(null!);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public async Task Validate_WhenUserIdIsValid_ShouldNotHaveError()
    {
        // Arrange
        var command = new DeleteUserCommand("user-id");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.UserId);
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new DeleteUserCommand("user-id");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenCommandWithEmailIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new DeleteUserCommand("user-id", UserEmail: "user@example.com");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
