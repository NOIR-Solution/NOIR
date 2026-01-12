namespace NOIR.Application.UnitTests.Features.Permissions;

/// <summary>
/// Unit tests for AssignPermissionToRoleCommandHandler.
/// Tests permission assignment scenarios with mocked dependencies.
/// </summary>
public class AssignPermissionToRoleCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRoleIdentityService> _roleIdentityServiceMock;
    private readonly Mock<ILocalizationService> _localizationServiceMock;
    private readonly AssignPermissionToRoleCommandHandler _handler;

    public AssignPermissionToRoleCommandHandlerTests()
    {
        _roleIdentityServiceMock = new Mock<IRoleIdentityService>();
        _localizationServiceMock = new Mock<ILocalizationService>();

        // Setup localization to return the key (pass-through for testing)
        _localizationServiceMock
            .Setup(x => x[It.IsAny<string>()])
            .Returns<string>(key => key);

        _handler = new AssignPermissionToRoleCommandHandler(
            _roleIdentityServiceMock.Object,
            _localizationServiceMock.Object);
    }

    private static RoleIdentityDto CreateTestRoleDto(
        string id = "role-123",
        string name = "TestRole")
    {
        return new RoleIdentityDto(id, name, name.ToUpperInvariant());
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidRoleAndPermissions_ShouldSucceed()
    {
        // Arrange
        const string roleId = "role-123";
        var permissions = new List<string> { "permissions.read", "permissions.write" };
        var updatedPermissions = new List<string> { "permissions.read", "permissions.write", "permissions.existing" };

        _roleIdentityServiceMock
            .Setup(x => x.FindByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRoleDto(roleId));

        _roleIdentityServiceMock
            .Setup(x => x.AddPermissionsAsync(roleId, permissions, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        _roleIdentityServiceMock
            .Setup(x => x.GetPermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedPermissions);

        var command = new AssignPermissionToRoleCommand(roleId, permissions);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(updatedPermissions);
    }

    [Fact]
    public async Task Handle_WithSinglePermission_ShouldSucceed()
    {
        // Arrange
        const string roleId = "role-123";
        var permissions = new List<string> { "permissions.read" };

        _roleIdentityServiceMock
            .Setup(x => x.FindByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRoleDto(roleId));

        _roleIdentityServiceMock
            .Setup(x => x.AddPermissionsAsync(roleId, permissions, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        _roleIdentityServiceMock
            .Setup(x => x.GetPermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);

        var command = new AssignPermissionToRoleCommand(roleId, permissions);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _roleIdentityServiceMock.Verify(
            x => x.AddPermissionsAsync(roleId, permissions, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Not Found Scenarios

    [Fact]
    public async Task Handle_WhenRoleNotFound_ShouldReturnNotFound()
    {
        // Arrange
        const string roleId = "non-existent-role";
        var permissions = new List<string> { "permissions.read" };

        _roleIdentityServiceMock
            .Setup(x => x.FindByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoleIdentityDto?)null);

        var command = new AssignPermissionToRoleCommand(roleId, permissions);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Auth.RoleNotFound);
        _roleIdentityServiceMock.Verify(
            x => x.AddPermissionsAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_WhenAddPermissionsFails_ShouldReturnValidationError()
    {
        // Arrange
        const string roleId = "role-123";
        var permissions = new List<string> { "invalid.permission" };

        _roleIdentityServiceMock
            .Setup(x => x.FindByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRoleDto(roleId));

        _roleIdentityServiceMock
            .Setup(x => x.AddPermissionsAsync(roleId, permissions, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Failure("Permission is invalid"));

        var command = new AssignPermissionToRoleCommand(roleId, permissions);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Validation.General);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToServices()
    {
        // Arrange
        const string roleId = "role-123";
        var permissions = new List<string> { "permissions.read" };

        _roleIdentityServiceMock
            .Setup(x => x.FindByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRoleDto(roleId));

        _roleIdentityServiceMock
            .Setup(x => x.AddPermissionsAsync(roleId, permissions, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        _roleIdentityServiceMock
            .Setup(x => x.GetPermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);

        var command = new AssignPermissionToRoleCommand(roleId, permissions);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await _handler.Handle(command, token);

        // Assert
        _roleIdentityServiceMock.Verify(x => x.FindByIdAsync(roleId, token), Times.Once);
        _roleIdentityServiceMock.Verify(x => x.AddPermissionsAsync(roleId, permissions, token), Times.Once);
        _roleIdentityServiceMock.Verify(x => x.GetPermissionsAsync(roleId, token), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnUpdatedPermissionsFromService()
    {
        // Arrange
        const string roleId = "role-123";
        var permissionsToAdd = new List<string> { "new.permission" };
        var existingPermissions = new List<string> { "existing.permission" };
        var expectedPermissions = new List<string> { "existing.permission", "new.permission" };

        _roleIdentityServiceMock
            .Setup(x => x.FindByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRoleDto(roleId));

        _roleIdentityServiceMock
            .Setup(x => x.AddPermissionsAsync(roleId, permissionsToAdd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        _roleIdentityServiceMock
            .Setup(x => x.GetPermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPermissions);

        var command = new AssignPermissionToRoleCommand(roleId, permissionsToAdd);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain("existing.permission");
        result.Value.Should().Contain("new.permission");
    }

    #endregion
}
