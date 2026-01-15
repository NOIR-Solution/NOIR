namespace NOIR.Application.UnitTests.Features.Roles;

/// <summary>
/// Unit tests for CreateRoleCommandHandler.
/// Tests role creation scenarios with mocked dependencies.
/// </summary>
public class CreateRoleCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRoleIdentityService> _roleIdentityServiceMock;
    private readonly Mock<ILocalizationService> _localizationServiceMock;
    private readonly CreateRoleCommandHandler _handler;

    public CreateRoleCommandHandlerTests()
    {
        _roleIdentityServiceMock = new Mock<IRoleIdentityService>();
        _localizationServiceMock = new Mock<ILocalizationService>();

        // Setup localization to return the key (pass-through for testing)
        _localizationServiceMock
            .Setup(x => x[It.IsAny<string>()])
            .Returns<string>(key => key);

        _handler = new CreateRoleCommandHandler(
            _roleIdentityServiceMock.Object,
            _localizationServiceMock.Object);
    }

    private static RoleIdentityDto CreateTestRoleDto(
        string id = "role-123",
        string name = "TestRole")
    {
        return new RoleIdentityDto(
            id,
            name,
            name.ToUpperInvariant(),
            Description: null,
            ParentRoleId: null,
            TenantId: null,
            IsSystemRole: false,
            SortOrder: 0,
            IconName: null,
            Color: null);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidName_ShouldSucceed()
    {
        // Arrange
        const string roleName = "NewRole";
        const string roleId = "role-123";

        _roleIdentityServiceMock
            .Setup(x => x.FindByNameAsync(roleName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoleIdentityDto?)null);

        _roleIdentityServiceMock
            .Setup(x => x.CreateRoleAsync(
                roleName,
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<Guid?>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        _roleIdentityServiceMock
            .SetupSequence(x => x.FindByNameAsync(roleName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoleIdentityDto?)null) // First call - check existence
            .ReturnsAsync(CreateTestRoleDto(roleId, roleName)); // Second call - get created role

        _roleIdentityServiceMock
            .Setup(x => x.GetPermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        _roleIdentityServiceMock
            .Setup(x => x.GetEffectivePermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        var command = new CreateRoleCommand(roleName);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(roleName);
        result.Value.UserCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithPermissions_ShouldAssignPermissions()
    {
        // Arrange
        const string roleName = "NewRole";
        const string roleId = "role-123";
        var permissions = new List<string> { "permissions.read", "permissions.write" };

        _roleIdentityServiceMock
            .SetupSequence(x => x.FindByNameAsync(roleName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoleIdentityDto?)null) // First call - check existence
            .ReturnsAsync(CreateTestRoleDto(roleId, roleName)); // Second call - get created role

        _roleIdentityServiceMock
            .Setup(x => x.CreateRoleAsync(
                roleName,
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<Guid?>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        _roleIdentityServiceMock
            .Setup(x => x.AddPermissionsAsync(roleId, permissions, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        _roleIdentityServiceMock
            .Setup(x => x.GetPermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);

        _roleIdentityServiceMock
            .Setup(x => x.GetEffectivePermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);

        var command = new CreateRoleCommand(roleName, Permissions: permissions);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Permissions.Should().BeEquivalentTo(permissions);
        _roleIdentityServiceMock.Verify(
            x => x.AddPermissionsAsync(roleId, permissions, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullPermissions_ShouldNotCallAddPermissions()
    {
        // Arrange
        const string roleName = "NewRole";
        const string roleId = "role-123";

        _roleIdentityServiceMock
            .SetupSequence(x => x.FindByNameAsync(roleName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoleIdentityDto?)null)
            .ReturnsAsync(CreateTestRoleDto(roleId, roleName));

        _roleIdentityServiceMock
            .Setup(x => x.CreateRoleAsync(
                roleName,
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<Guid?>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        _roleIdentityServiceMock
            .Setup(x => x.GetPermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        _roleIdentityServiceMock
            .Setup(x => x.GetEffectivePermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        var command = new CreateRoleCommand(roleName, null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _roleIdentityServiceMock.Verify(
            x => x.AddPermissionsAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithEmptyPermissions_ShouldNotCallAddPermissions()
    {
        // Arrange
        const string roleName = "NewRole";
        const string roleId = "role-123";

        _roleIdentityServiceMock
            .SetupSequence(x => x.FindByNameAsync(roleName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoleIdentityDto?)null)
            .ReturnsAsync(CreateTestRoleDto(roleId, roleName));

        _roleIdentityServiceMock
            .Setup(x => x.CreateRoleAsync(
                roleName,
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<Guid?>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        _roleIdentityServiceMock
            .Setup(x => x.GetPermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        _roleIdentityServiceMock
            .Setup(x => x.GetEffectivePermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        var command = new CreateRoleCommand(roleName, Permissions: new List<string>());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _roleIdentityServiceMock.Verify(
            x => x.AddPermissionsAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Conflict Scenarios

    [Fact]
    public async Task Handle_WhenRoleAlreadyExists_ShouldReturnConflict()
    {
        // Arrange
        const string roleName = "ExistingRole";

        _roleIdentityServiceMock
            .Setup(x => x.FindByNameAsync(roleName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRoleDto("existing-id", roleName));

        var command = new CreateRoleCommand(roleName);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Business.AlreadyExists);
        _roleIdentityServiceMock.Verify(
            x => x.CreateRoleAsync(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<Guid?>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Creation Failure Scenarios

    [Fact]
    public async Task Handle_WhenCreateRoleFails_ShouldReturnValidationError()
    {
        // Arrange
        const string roleName = "NewRole";

        _roleIdentityServiceMock
            .Setup(x => x.FindByNameAsync(roleName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoleIdentityDto?)null);

        _roleIdentityServiceMock
            .Setup(x => x.CreateRoleAsync(
                roleName,
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<Guid?>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Failure("Role name is invalid"));

        var command = new CreateRoleCommand(roleName);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Validation.General);
    }

    [Fact]
    public async Task Handle_WhenRetrievalAfterCreateFails_ShouldReturnUnknownError()
    {
        // Arrange
        const string roleName = "NewRole";

        _roleIdentityServiceMock
            .SetupSequence(x => x.FindByNameAsync(roleName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoleIdentityDto?)null) // First call - check existence
            .ReturnsAsync((RoleIdentityDto?)null); // Second call - retrieval fails

        _roleIdentityServiceMock
            .Setup(x => x.CreateRoleAsync(
                roleName,
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<Guid?>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        var command = new CreateRoleCommand(roleName);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.System.UnknownError);
    }

    [Fact]
    public async Task Handle_WhenAddPermissionsFails_ShouldReturnValidationError()
    {
        // Arrange
        const string roleName = "NewRole";
        const string roleId = "role-123";
        var permissions = new List<string> { "invalid.permission" };

        _roleIdentityServiceMock
            .SetupSequence(x => x.FindByNameAsync(roleName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoleIdentityDto?)null)
            .ReturnsAsync(CreateTestRoleDto(roleId, roleName));

        _roleIdentityServiceMock
            .Setup(x => x.CreateRoleAsync(
                roleName,
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<Guid?>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        _roleIdentityServiceMock
            .Setup(x => x.AddPermissionsAsync(roleId, permissions, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Failure("Invalid permission"));

        var command = new CreateRoleCommand(roleName, Permissions: permissions);

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
        const string roleName = "NewRole";
        const string roleId = "role-123";

        _roleIdentityServiceMock
            .SetupSequence(x => x.FindByNameAsync(roleName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoleIdentityDto?)null)
            .ReturnsAsync(CreateTestRoleDto(roleId, roleName));

        _roleIdentityServiceMock
            .Setup(x => x.CreateRoleAsync(
                roleName,
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<Guid?>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        _roleIdentityServiceMock
            .Setup(x => x.GetPermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        _roleIdentityServiceMock
            .Setup(x => x.GetEffectivePermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        var command = new CreateRoleCommand(roleName);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await _handler.Handle(command, token);

        // Assert
        _roleIdentityServiceMock.Verify(x => x.FindByNameAsync(roleName, token), Times.AtLeastOnce);
        _roleIdentityServiceMock.Verify(
            x => x.CreateRoleAsync(
                roleName,
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<Guid?>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                token),
            Times.Once);
        _roleIdentityServiceMock.Verify(x => x.GetPermissionsAsync(roleId, token), Times.Once);
        _roleIdentityServiceMock.Verify(x => x.GetEffectivePermissionsAsync(roleId, token), Times.Once);
    }

    #endregion
}
