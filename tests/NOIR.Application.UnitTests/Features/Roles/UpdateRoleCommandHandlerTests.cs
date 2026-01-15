namespace NOIR.Application.UnitTests.Features.Roles;

/// <summary>
/// Unit tests for UpdateRoleCommandHandler.
/// Tests role update scenarios with mocked dependencies.
/// </summary>
public class UpdateRoleCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRoleIdentityService> _roleIdentityServiceMock;
    private readonly Mock<ILocalizationService> _localizationServiceMock;
    private readonly UpdateRoleCommandHandler _handler;

    public UpdateRoleCommandHandlerTests()
    {
        _roleIdentityServiceMock = new Mock<IRoleIdentityService>();
        _localizationServiceMock = new Mock<ILocalizationService>();

        // Setup localization to return the key (pass-through for testing)
        _localizationServiceMock
            .Setup(x => x[It.IsAny<string>()])
            .Returns<string>(key => key);

        _handler = new UpdateRoleCommandHandler(
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
    public async Task Handle_WithValidData_ShouldSucceed()
    {
        // Arrange
        const string roleId = "role-123";
        const string oldName = "OldRole";
        const string newName = "NewRole";

        _roleIdentityServiceMock
            .Setup(x => x.FindByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRoleDto(roleId, oldName));

        _roleIdentityServiceMock
            .Setup(x => x.FindByNameAsync(newName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoleIdentityDto?)null);

        _roleIdentityServiceMock
            .Setup(x => x.UpdateRoleAsync(
                roleId,
                newName,
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        _roleIdentityServiceMock
            .SetupSequence(x => x.FindByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRoleDto(roleId, oldName)) // First call - verify existence
            .ReturnsAsync(CreateTestRoleDto(roleId, newName)); // Second call - get updated role

        _roleIdentityServiceMock
            .Setup(x => x.GetPermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "permissions.read" });

        _roleIdentityServiceMock
            .Setup(x => x.GetEffectivePermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "permissions.read" });

        _roleIdentityServiceMock
            .Setup(x => x.GetUserCountAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var command = new UpdateRoleCommand(roleId, newName);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(newName);
        result.Value.UserCount.Should().Be(5);
        result.Value.Permissions.Should().Contain("permissions.read");
    }

    [Fact]
    public async Task Handle_WhenRenamingToSameName_ShouldSucceed()
    {
        // Arrange
        const string roleId = "role-123";
        const string roleName = "SameRole";

        _roleIdentityServiceMock
            .SetupSequence(x => x.FindByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRoleDto(roleId, roleName))
            .ReturnsAsync(CreateTestRoleDto(roleId, roleName));

        // FindByName returns the same role (same ID)
        _roleIdentityServiceMock
            .Setup(x => x.FindByNameAsync(roleName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRoleDto(roleId, roleName));

        _roleIdentityServiceMock
            .Setup(x => x.UpdateRoleAsync(
                roleId,
                roleName,
                It.IsAny<string?>(),
                It.IsAny<string?>(),
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

        _roleIdentityServiceMock
            .Setup(x => x.GetUserCountAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var command = new UpdateRoleCommand(roleId, roleName);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Not Found Scenarios

    [Fact]
    public async Task Handle_WhenRoleNotFound_ShouldReturnNotFound()
    {
        // Arrange
        const string roleId = "non-existent-role";
        const string newName = "NewRole";

        _roleIdentityServiceMock
            .Setup(x => x.FindByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoleIdentityDto?)null);

        var command = new UpdateRoleCommand(roleId, newName);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Auth.RoleNotFound);
        _roleIdentityServiceMock.Verify(
            x => x.UpdateRoleAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Conflict Scenarios

    [Fact]
    public async Task Handle_WhenNameConflictsWithAnotherRole_ShouldReturnConflict()
    {
        // Arrange
        const string roleId = "role-123";
        const string conflictingRoleId = "role-456";
        const string newName = "ExistingRole";

        _roleIdentityServiceMock
            .Setup(x => x.FindByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRoleDto(roleId, "OldRole"));

        // Another role already has the new name
        _roleIdentityServiceMock
            .Setup(x => x.FindByNameAsync(newName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRoleDto(conflictingRoleId, newName));

        var command = new UpdateRoleCommand(roleId, newName);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Business.AlreadyExists);
        _roleIdentityServiceMock.Verify(
            x => x.UpdateRoleAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Update Failure Scenarios

    [Fact]
    public async Task Handle_WhenUpdateFails_ShouldReturnValidationError()
    {
        // Arrange
        const string roleId = "role-123";
        const string newName = "NewRole";

        _roleIdentityServiceMock
            .Setup(x => x.FindByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRoleDto(roleId, "OldRole"));

        _roleIdentityServiceMock
            .Setup(x => x.FindByNameAsync(newName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoleIdentityDto?)null);

        _roleIdentityServiceMock
            .Setup(x => x.UpdateRoleAsync(
                roleId,
                newName,
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Failure("Update failed"));

        var command = new UpdateRoleCommand(roleId, newName);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Validation.General);
    }

    [Fact]
    public async Task Handle_WhenRetrievalAfterUpdateFails_ShouldReturnUnknownError()
    {
        // Arrange
        const string roleId = "role-123";
        const string newName = "NewRole";

        _roleIdentityServiceMock
            .SetupSequence(x => x.FindByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRoleDto(roleId, "OldRole")) // First call - verify existence
            .ReturnsAsync((RoleIdentityDto?)null); // Second call - retrieval fails

        _roleIdentityServiceMock
            .Setup(x => x.FindByNameAsync(newName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoleIdentityDto?)null);

        _roleIdentityServiceMock
            .Setup(x => x.UpdateRoleAsync(
                roleId,
                newName,
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        var command = new UpdateRoleCommand(roleId, newName);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.System.UnknownError);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToServices()
    {
        // Arrange
        const string roleId = "role-123";
        const string newName = "NewRole";

        _roleIdentityServiceMock
            .SetupSequence(x => x.FindByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRoleDto(roleId, "OldRole"))
            .ReturnsAsync(CreateTestRoleDto(roleId, newName));

        _roleIdentityServiceMock
            .Setup(x => x.FindByNameAsync(newName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoleIdentityDto?)null);

        _roleIdentityServiceMock
            .Setup(x => x.UpdateRoleAsync(
                roleId,
                newName,
                It.IsAny<string?>(),
                It.IsAny<string?>(),
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

        _roleIdentityServiceMock
            .Setup(x => x.GetUserCountAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var command = new UpdateRoleCommand(roleId, newName);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await _handler.Handle(command, token);

        // Assert
        _roleIdentityServiceMock.Verify(x => x.FindByIdAsync(roleId, token), Times.AtLeastOnce);
        _roleIdentityServiceMock.Verify(
            x => x.UpdateRoleAsync(
                roleId,
                newName,
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                token),
            Times.Once);
        _roleIdentityServiceMock.Verify(x => x.GetPermissionsAsync(roleId, token), Times.Once);
        _roleIdentityServiceMock.Verify(x => x.GetEffectivePermissionsAsync(roleId, token), Times.Once);
        _roleIdentityServiceMock.Verify(x => x.GetUserCountAsync(roleId, token), Times.Once);
    }

    #endregion
}
