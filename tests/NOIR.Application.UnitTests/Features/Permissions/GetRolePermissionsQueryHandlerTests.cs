namespace NOIR.Application.UnitTests.Features.Permissions;

/// <summary>
/// Unit tests for GetRolePermissionsQueryHandler.
/// Tests role permission retrieval scenarios with mocked dependencies.
/// </summary>
public class GetRolePermissionsQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRoleIdentityService> _roleIdentityServiceMock;
    private readonly Mock<ILocalizationService> _localizationServiceMock;
    private readonly GetRolePermissionsQueryHandler _handler;

    public GetRolePermissionsQueryHandlerTests()
    {
        _roleIdentityServiceMock = new Mock<IRoleIdentityService>();
        _localizationServiceMock = new Mock<ILocalizationService>();

        // Setup localization to return the key (pass-through for testing)
        _localizationServiceMock
            .Setup(x => x[It.IsAny<string>()])
            .Returns<string>(key => key);

        _handler = new GetRolePermissionsQueryHandler(
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
    public async Task Handle_WithValidRole_ShouldReturnPermissions()
    {
        // Arrange
        const string roleId = "role-123";
        var permissions = new List<string> { "users.read", "users.write", "roles.read" };

        _roleIdentityServiceMock
            .Setup(x => x.FindByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRoleDto(roleId));

        _roleIdentityServiceMock
            .Setup(x => x.GetPermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);

        var query = new GetRolePermissionsQuery(roleId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value.Should().BeEquivalentTo(permissions);
    }

    [Fact]
    public async Task Handle_WithSinglePermission_ShouldReturnSinglePermission()
    {
        // Arrange
        const string roleId = "role-123";
        var permissions = new List<string> { "users.read" };

        _roleIdentityServiceMock
            .Setup(x => x.FindByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRoleDto(roleId));

        _roleIdentityServiceMock
            .Setup(x => x.GetPermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);

        var query = new GetRolePermissionsQuery(roleId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle().Which.Should().Be("users.read");
    }

    [Fact]
    public async Task Handle_WithManyPermissions_ShouldReturnAllPermissions()
    {
        // Arrange
        const string roleId = "role-admin";
        var permissions = new List<string>
        {
            "users.read",
            "users.write",
            "users.delete",
            "roles.read",
            "roles.write",
            "tenants.read",
            "system.admin"
        };

        _roleIdentityServiceMock
            .Setup(x => x.FindByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRoleDto(roleId, "Administrator"));

        _roleIdentityServiceMock
            .Setup(x => x.GetPermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);

        var query = new GetRolePermissionsQuery(roleId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(7);
        result.Value.Should().BeEquivalentTo(permissions);
    }

    #endregion

    #region Empty Results

    [Fact]
    public async Task Handle_WhenRoleHasNoPermissions_ShouldReturnEmptyList()
    {
        // Arrange
        const string roleId = "role-empty";

        _roleIdentityServiceMock
            .Setup(x => x.FindByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRoleDto(roleId, "EmptyRole"));

        _roleIdentityServiceMock
            .Setup(x => x.GetPermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        var query = new GetRolePermissionsQuery(roleId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEmpty();
    }

    #endregion

    #region Not Found Scenarios

    [Fact]
    public async Task Handle_WhenRoleNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        const string roleId = "non-existent-role";

        _roleIdentityServiceMock
            .Setup(x => x.FindByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoleIdentityDto?)null);

        var query = new GetRolePermissionsQuery(roleId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Auth.RoleNotFound);
        _roleIdentityServiceMock.Verify(
            x => x.GetPermissionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRoleNotFound_ShouldNotAttemptToGetPermissions()
    {
        // Arrange
        const string roleId = "invalid-role-id";

        _roleIdentityServiceMock
            .Setup(x => x.FindByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoleIdentityDto?)null);

        var query = new GetRolePermissionsQuery(roleId);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _roleIdentityServiceMock.Verify(x => x.FindByIdAsync(roleId, It.IsAny<CancellationToken>()), Times.Once);
        _roleIdentityServiceMock.Verify(x => x.GetPermissionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToServices()
    {
        // Arrange
        const string roleId = "role-123";
        var permissions = new List<string> { "users.read" };

        _roleIdentityServiceMock
            .Setup(x => x.FindByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRoleDto(roleId));

        _roleIdentityServiceMock
            .Setup(x => x.GetPermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);

        var query = new GetRolePermissionsQuery(roleId);
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await _handler.Handle(query, token);

        // Assert
        _roleIdentityServiceMock.Verify(x => x.FindByIdAsync(roleId, token), Times.Once);
        _roleIdentityServiceMock.Verify(x => x.GetPermissionsAsync(roleId, token), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldVerifyRoleExistsBeforeGettingPermissions()
    {
        // Arrange
        const string roleId = "role-123";
        var permissions = new List<string> { "users.read" };
        var callOrder = new List<string>();

        _roleIdentityServiceMock
            .Setup(x => x.FindByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("FindByIdAsync"))
            .ReturnsAsync(CreateTestRoleDto(roleId));

        _roleIdentityServiceMock
            .Setup(x => x.GetPermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("GetPermissionsAsync"))
            .ReturnsAsync(permissions);

        var query = new GetRolePermissionsQuery(roleId);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        callOrder.Should().ContainInOrder("FindByIdAsync", "GetPermissionsAsync");
    }

    [Fact]
    public async Task Handle_WithDifferentRoleIds_ShouldQueryCorrectRole()
    {
        // Arrange
        const string roleId1 = "role-admin";
        const string roleId2 = "role-user";
        var adminPermissions = new List<string> { "admin.full" };
        var userPermissions = new List<string> { "users.read" };

        _roleIdentityServiceMock
            .Setup(x => x.FindByIdAsync(roleId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRoleDto(roleId1, "Admin"));

        _roleIdentityServiceMock
            .Setup(x => x.FindByIdAsync(roleId2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRoleDto(roleId2, "User"));

        _roleIdentityServiceMock
            .Setup(x => x.GetPermissionsAsync(roleId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminPermissions);

        _roleIdentityServiceMock
            .Setup(x => x.GetPermissionsAsync(roleId2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userPermissions);

        var query1 = new GetRolePermissionsQuery(roleId1);
        var query2 = new GetRolePermissionsQuery(roleId2);

        // Act
        var result1 = await _handler.Handle(query1, CancellationToken.None);
        var result2 = await _handler.Handle(query2, CancellationToken.None);

        // Assert
        result1.Value.Should().ContainSingle().Which.Should().Be("admin.full");
        result2.Value.Should().ContainSingle().Which.Should().Be("users.read");
    }

    #endregion
}
