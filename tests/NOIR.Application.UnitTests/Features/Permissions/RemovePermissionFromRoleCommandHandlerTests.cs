namespace NOIR.Application.UnitTests.Features.Permissions;

/// <summary>
/// Unit tests for RemovePermissionFromRoleCommandHandler.
/// Tests permission removal scenarios with mocked dependencies.
/// </summary>
public class RemovePermissionFromRoleCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRoleIdentityService> _roleIdentityServiceMock;
    private readonly Mock<ILocalizationService> _localizationServiceMock;
    private readonly Mock<IPermissionCacheInvalidator> _cacheInvalidatorMock;
    private readonly RemovePermissionFromRoleCommandHandler _handler;

    public RemovePermissionFromRoleCommandHandlerTests()
    {
        _roleIdentityServiceMock = new Mock<IRoleIdentityService>();
        _localizationServiceMock = new Mock<ILocalizationService>();
        _cacheInvalidatorMock = new Mock<IPermissionCacheInvalidator>();

        // Setup localization to return the key (pass-through for testing)
        _localizationServiceMock
            .Setup(x => x[It.IsAny<string>()])
            .Returns<string>(key => key);

        _handler = new RemovePermissionFromRoleCommandHandler(
            _roleIdentityServiceMock.Object,
            _localizationServiceMock.Object,
            _cacheInvalidatorMock.Object);
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
        var permissionsToRemove = new List<string> { "permissions.write" };
        var remainingPermissions = new List<string> { "permissions.read" };

        _roleIdentityServiceMock
            .Setup(x => x.FindByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRoleDto(roleId));

        _roleIdentityServiceMock
            .Setup(x => x.RemovePermissionsAsync(roleId, permissionsToRemove, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        _roleIdentityServiceMock
            .Setup(x => x.GetPermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(remainingPermissions);

        var command = new RemovePermissionFromRoleCommand(roleId, permissionsToRemove);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(remainingPermissions);
    }

    [Fact]
    public async Task Handle_WithMultiplePermissions_ShouldRemoveAll()
    {
        // Arrange
        const string roleId = "role-123";
        var permissionsToRemove = new List<string> { "permissions.write", "permissions.delete" };
        var remainingPermissions = new List<string> { "permissions.read" };

        _roleIdentityServiceMock
            .Setup(x => x.FindByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRoleDto(roleId));

        _roleIdentityServiceMock
            .Setup(x => x.RemovePermissionsAsync(roleId, permissionsToRemove, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        _roleIdentityServiceMock
            .Setup(x => x.GetPermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(remainingPermissions);

        var command = new RemovePermissionFromRoleCommand(roleId, permissionsToRemove);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _roleIdentityServiceMock.Verify(
            x => x.RemovePermissionsAsync(roleId, permissionsToRemove, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_RemoveAllPermissions_ShouldReturnEmptyList()
    {
        // Arrange
        const string roleId = "role-123";
        var permissionsToRemove = new List<string> { "permissions.read", "permissions.write" };
        var emptyPermissions = new List<string>();

        _roleIdentityServiceMock
            .Setup(x => x.FindByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRoleDto(roleId));

        _roleIdentityServiceMock
            .Setup(x => x.RemovePermissionsAsync(roleId, permissionsToRemove, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        _roleIdentityServiceMock
            .Setup(x => x.GetPermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyPermissions);

        var command = new RemovePermissionFromRoleCommand(roleId, permissionsToRemove);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
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

        var command = new RemovePermissionFromRoleCommand(roleId, permissions);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Auth.RoleNotFound);
        _roleIdentityServiceMock.Verify(
            x => x.RemovePermissionsAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_WhenRemovePermissionsFails_ShouldReturnValidationError()
    {
        // Arrange
        const string roleId = "role-123";
        var permissions = new List<string> { "permissions.read" };

        _roleIdentityServiceMock
            .Setup(x => x.FindByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRoleDto(roleId));

        _roleIdentityServiceMock
            .Setup(x => x.RemovePermissionsAsync(roleId, permissions, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Failure("Cannot remove permission"));

        var command = new RemovePermissionFromRoleCommand(roleId, permissions);

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
            .Setup(x => x.RemovePermissionsAsync(roleId, permissions, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        _roleIdentityServiceMock
            .Setup(x => x.GetPermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        var command = new RemovePermissionFromRoleCommand(roleId, permissions);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await _handler.Handle(command, token);

        // Assert
        _roleIdentityServiceMock.Verify(x => x.FindByIdAsync(roleId, token), Times.Once);
        _roleIdentityServiceMock.Verify(x => x.RemovePermissionsAsync(roleId, permissions, token), Times.Once);
        _roleIdentityServiceMock.Verify(x => x.GetPermissionsAsync(roleId, token), Times.Once);
    }

    #endregion
}
