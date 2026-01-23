namespace NOIR.Application.UnitTests.Features.Users.Validators;

/// <summary>
/// Unit tests for LockUserCommandValidator.
/// Tests all validation rules for locking a user.
/// </summary>
public class LockUserCommandValidatorTests
{
    private readonly LockUserCommandValidator _validator;

    public LockUserCommandValidatorTests()
    {
        _validator = new LockUserCommandValidator(CreateLocalizationMock());
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
        var command = new LockUserCommand("", Lock: true);

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
        var command = new LockUserCommand(null!, Lock: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public async Task Validate_WhenUserIdIsValid_ShouldNotHaveError()
    {
        // Arrange
        var command = new LockUserCommand("user-id", Lock: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.UserId);
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenLockCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new LockUserCommand("user-id", Lock: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenUnlockCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new LockUserCommand("user-id", Lock: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenCommandWithEmailIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new LockUserCommand("user-id", Lock: true, UserEmail: "user@example.com");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
