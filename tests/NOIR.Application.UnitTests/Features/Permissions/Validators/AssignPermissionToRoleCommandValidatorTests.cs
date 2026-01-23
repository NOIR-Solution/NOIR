namespace NOIR.Application.UnitTests.Features.Permissions.Validators;

using NOIR.Application.Features.Permissions.Commands.AssignToRole;
using DomainPermissions = NOIR.Domain.Common.Permissions;

/// <summary>
/// Unit tests for AssignPermissionToRoleCommandValidator.
/// Tests validation rules for assigning permissions to a role.
/// </summary>
public class AssignPermissionToRoleCommandValidatorTests
{
    private readonly AssignPermissionToRoleCommandValidator _validator;

    public AssignPermissionToRoleCommandValidatorTests()
    {
        _validator = new AssignPermissionToRoleCommandValidator(CreateLocalizationMock());
    }

    /// <summary>
    /// Creates a mock ILocalizationService that returns the expected English messages.
    /// </summary>
    private static ILocalizationService CreateLocalizationMock()
    {
        var mock = new Mock<ILocalizationService>();

        mock.Setup(x => x["validation.roleId.required"]).Returns("Role ID is required.");
        mock.Setup(x => x["validation.permissions.required"]).Returns("Permissions list is required.");
        mock.Setup(x => x["validation.permissions.empty"]).Returns("Permission cannot be empty.");
        mock.Setup(x => x.Get("validation.permissions.invalid", It.IsAny<object[]>()))
            .Returns((string _, object[] args) => $"Permission '{args[0]}' is not a valid permission.");

        return mock.Object;
    }

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new AssignPermissionToRoleCommand(
            RoleId: Guid.NewGuid().ToString(),
            Permissions: new List<string> { DomainPermissions.UsersRead, DomainPermissions.UsersCreate },
            RoleName: "TestRole");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenSingleValidPermission_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new AssignPermissionToRoleCommand(
            RoleId: Guid.NewGuid().ToString(),
            Permissions: new List<string> { DomainPermissions.RolesRead });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenEmptyPermissionsList_ShouldNotHaveError()
    {
        // Arrange - AssignPermissionToRoleCommand allows empty list (unlike RemoveFromRole)
        var command = new AssignPermissionToRoleCommand(
            RoleId: Guid.NewGuid().ToString(),
            Permissions: new List<string>());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Permissions);
    }

    #endregion

    #region RoleId Validation

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_WhenRoleIdIsEmptyOrWhitespace_ShouldHaveError(string? roleId)
    {
        // Arrange
        var command = new AssignPermissionToRoleCommand(
            RoleId: roleId!,
            Permissions: new List<string> { DomainPermissions.UsersRead });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RoleId)
            .WithErrorMessage("Role ID is required.");
    }

    [Fact]
    public async Task Validate_WhenRoleIdIsValid_ShouldNotHaveError()
    {
        // Arrange
        var command = new AssignPermissionToRoleCommand(
            RoleId: Guid.NewGuid().ToString(),
            Permissions: new List<string> { DomainPermissions.UsersRead });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.RoleId);
    }

    #endregion

    #region Permissions List Validation

    [Fact]
    public async Task Validate_WhenPermissionsIsNull_ShouldHaveError()
    {
        // Arrange
        var command = new AssignPermissionToRoleCommand(
            RoleId: Guid.NewGuid().ToString(),
            Permissions: null!);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Permissions)
            .WithErrorMessage("Permissions list is required.");
    }

    #endregion

    #region Individual Permission Validation

    [Fact]
    public async Task Validate_WhenPermissionIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new AssignPermissionToRoleCommand(
            RoleId: Guid.NewGuid().ToString(),
            Permissions: new List<string> { "" });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Permission cannot be empty.");
    }

    [Fact]
    public async Task Validate_WhenPermissionIsWhitespace_ShouldHaveError()
    {
        // Arrange
        var command = new AssignPermissionToRoleCommand(
            RoleId: Guid.NewGuid().ToString(),
            Permissions: new List<string> { "   " });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Permission cannot be empty.");
    }

    [Fact]
    public async Task Validate_WhenPermissionIsInvalid_ShouldHaveError()
    {
        // Arrange
        var command = new AssignPermissionToRoleCommand(
            RoleId: Guid.NewGuid().ToString(),
            Permissions: new List<string> { "invalid:permission" });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Permission 'invalid:permission' is not a valid permission.");
    }

    [Fact]
    public async Task Validate_WhenMixedValidAndInvalidPermissions_ShouldHaveError()
    {
        // Arrange
        var command = new AssignPermissionToRoleCommand(
            RoleId: Guid.NewGuid().ToString(),
            Permissions: new List<string> { DomainPermissions.UsersRead, "invalid:permission" });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Permission 'invalid:permission' is not a valid permission.");
    }

    [Theory]
    [InlineData("users:read")]
    [InlineData("roles:create")]
    [InlineData("system:admin")]
    public async Task Validate_WhenPermissionIsValid_ShouldNotHaveError(string permission)
    {
        // Arrange
        var command = new AssignPermissionToRoleCommand(
            RoleId: Guid.NewGuid().ToString(),
            Permissions: new List<string> { permission });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
