namespace NOIR.Application.UnitTests.Features.Roles.Validators;

/// <summary>
/// Unit tests for DeleteRoleCommandValidator.
/// Tests validation rules for role deletion.
/// </summary>
public class DeleteRoleCommandValidatorTests
{
    private readonly DeleteRoleCommandValidator _validator;

    public DeleteRoleCommandValidatorTests()
    {
        _validator = new DeleteRoleCommandValidator(CreateLocalizationMock());
    }

    /// <summary>
    /// Creates a mock ILocalizationService that returns the expected validation messages.
    /// </summary>
    private static ILocalizationService CreateLocalizationMock()
    {
        var mock = new Mock<ILocalizationService>();

        // RoleId validation
        mock.Setup(x => x["validation.roleId.required"]).Returns("Role ID is required.");

        return mock.Object;
    }

    #region RoleId Validation

    [Fact]
    public async Task Validate_WhenRoleIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new DeleteRoleCommand("");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RoleId)
            .WithErrorMessage("Role ID is required.");
    }

    [Fact]
    public async Task Validate_WhenRoleIdIsNull_ShouldHaveError()
    {
        // Arrange
        var command = new DeleteRoleCommand(null!);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RoleId);
    }

    [Fact]
    public async Task Validate_WhenRoleIdIsWhitespace_ShouldHaveError()
    {
        // Arrange
        var command = new DeleteRoleCommand("   ");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RoleId);
    }

    #endregion

    #region Valid RoleId Tests

    [Theory]
    [InlineData("role-123")]
    [InlineData("abc")]
    [InlineData("550e8400-e29b-41d4-a716-446655440000")]
    [InlineData("some-role-id-value")]
    public async Task Validate_WhenRoleIdIsValid_ShouldNotHaveError(string roleId)
    {
        // Arrange
        var command = new DeleteRoleCommand(roleId);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new DeleteRoleCommand("role-123");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenCommandIsValidWithRoleName_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new DeleteRoleCommand("role-123", RoleName: "Admin");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenCommandIsValidWithNullRoleName_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new DeleteRoleCommand("role-123", RoleName: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
