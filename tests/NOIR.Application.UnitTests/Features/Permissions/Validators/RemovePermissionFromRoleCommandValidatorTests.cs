namespace NOIR.Application.UnitTests.Features.Permissions.Validators;

using NOIR.Application.Features.Permissions.Commands.RemoveFromRole;
using DomainPermissions = NOIR.Domain.Common.Permissions;

/// <summary>
/// Unit tests for RemovePermissionFromRoleCommandValidator.
/// Tests validation rules for removing permissions from a role.
/// </summary>
public class RemovePermissionFromRoleCommandValidatorTests
{
    private readonly RemovePermissionFromRoleCommandValidator _validator;

    public RemovePermissionFromRoleCommandValidatorTests()
    {
        _validator = new RemovePermissionFromRoleCommandValidator(CreateLocalizationMock());
    }

    /// <summary>
    /// Creates a mock ILocalizationService that returns the expected English messages.
    /// </summary>
    private static ILocalizationService CreateLocalizationMock()
    {
        var mock = new Mock<ILocalizationService>();

        mock.Setup(x => x["validation.roleId.required"]).Returns("Role ID is required.");
        mock.Setup(x => x["validation.permissions.required"]).Returns("Permissions list is required.");
        mock.Setup(x => x["validation.permissions.minOne"]).Returns("At least one permission must be specified.");
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
        var command = new RemovePermissionFromRoleCommand(
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
        var command = new RemovePermissionFromRoleCommand(
            RoleId: Guid.NewGuid().ToString(),
            Permissions: new List<string> { DomainPermissions.RolesRead });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
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
        var command = new RemovePermissionFromRoleCommand(
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
        var command = new RemovePermissionFromRoleCommand(
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
        var command = new RemovePermissionFromRoleCommand(
            RoleId: Guid.NewGuid().ToString(),
            Permissions: null!);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Permissions)
            .WithErrorMessage("Permissions list is required.");
    }

    [Fact]
    public async Task Validate_WhenPermissionsIsEmpty_ShouldHaveError()
    {
        // Arrange - RemoveFromRole requires at least one permission (unlike Assign)
        var command = new RemovePermissionFromRoleCommand(
            RoleId: Guid.NewGuid().ToString(),
            Permissions: new List<string>());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Permissions)
            .WithErrorMessage("At least one permission must be specified.");
    }

    #endregion

    #region Individual Permission Validation

    [Fact]
    public async Task Validate_WhenPermissionIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new RemovePermissionFromRoleCommand(
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
        var command = new RemovePermissionFromRoleCommand(
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
        var command = new RemovePermissionFromRoleCommand(
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
        var command = new RemovePermissionFromRoleCommand(
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
        var command = new RemovePermissionFromRoleCommand(
            RoleId: Guid.NewGuid().ToString(),
            Permissions: new List<string> { permission });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Multiple Permissions Validation

    [Fact]
    public async Task Validate_WhenAllPermissionsAreValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new RemovePermissionFromRoleCommand(
            RoleId: Guid.NewGuid().ToString(),
            Permissions: new List<string>
            {
                DomainPermissions.UsersRead,
                DomainPermissions.UsersCreate,
                DomainPermissions.UsersUpdate,
                DomainPermissions.UsersDelete
            });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenMultiplePermissionsAreInvalid_ShouldHaveMultipleErrors()
    {
        // Arrange
        var command = new RemovePermissionFromRoleCommand(
            RoleId: Guid.NewGuid().ToString(),
            Permissions: new List<string> { "invalid:one", "invalid:two" });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    #endregion
}
