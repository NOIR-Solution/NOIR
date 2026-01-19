namespace NOIR.Application.UnitTests.Features.Permissions;

/// <summary>
/// Unit tests for GetUserPermissionsQueryHandler.
/// Tests user permission aggregation scenarios with mocked dependencies.
/// </summary>
public class GetUserPermissionsQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IUserIdentityService> _userIdentityServiceMock;
    private readonly Mock<IRoleIdentityService> _roleIdentityServiceMock;
    private readonly Mock<ILocalizationService> _localizationServiceMock;
    private readonly GetUserPermissionsQueryHandler _handler;

    public GetUserPermissionsQueryHandlerTests()
    {
        _userIdentityServiceMock = new Mock<IUserIdentityService>();
        _roleIdentityServiceMock = new Mock<IRoleIdentityService>();
        _localizationServiceMock = new Mock<ILocalizationService>();

        // Setup localization to return the key (pass-through for testing)
        _localizationServiceMock
            .Setup(x => x[It.IsAny<string>()])
            .Returns<string>(key => key);

        _handler = new GetUserPermissionsQueryHandler(
            _userIdentityServiceMock.Object,
            _roleIdentityServiceMock.Object,
            _localizationServiceMock.Object);
    }

    private static UserIdentityDto CreateTestUserDto(
        string id = "user-123",
        string email = "test@example.com")
    {
        return new UserIdentityDto(
            Id: id,
            Email: email,
            TenantId: "default",
            FirstName: null,
            LastName: null,
            DisplayName: null,
            FullName: email,
            PhoneNumber: null,
            AvatarUrl: null,
            IsActive: true,
            IsDeleted: false,
            IsSystemUser: false,
            CreatedAt: DateTimeOffset.UtcNow,
            ModifiedAt: null);
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
    public async Task Handle_WithSingleRole_ShouldReturnRolePermissions()
    {
        // Arrange
        const string userId = "user-123";
        const string roleName = "Admin";
        const string roleId = "role-admin";
        var permissions = new List<string> { "users.read", "users.write" };

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(userId));

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { roleName });

        _roleIdentityServiceMock
            .Setup(x => x.FindByNameAsync(roleName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRoleDto(roleId, roleName));

        _roleIdentityServiceMock
            .Setup(x => x.GetPermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);

        var query = new GetUserPermissionsQuery(userId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(userId);
        result.Value.Permissions.Should().HaveCount(2);
        result.Value.Permissions.Should().Contain("users.read");
        result.Value.Permissions.Should().Contain("users.write");
    }

    [Fact]
    public async Task Handle_WithMultipleRoles_ShouldAggregatePermissions()
    {
        // Arrange
        const string userId = "user-123";
        var roles = new List<string> { "Admin", "Editor" };

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(userId));

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);

        _roleIdentityServiceMock
            .Setup(x => x.FindByNameAsync("Admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRoleDto("role-admin", "Admin"));

        _roleIdentityServiceMock
            .Setup(x => x.FindByNameAsync("Editor", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRoleDto("role-editor", "Editor"));

        _roleIdentityServiceMock
            .Setup(x => x.GetPermissionsAsync("role-admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "users.read", "users.write" });

        _roleIdentityServiceMock
            .Setup(x => x.GetPermissionsAsync("role-editor", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "content.read", "content.write" });

        var query = new GetUserPermissionsQuery(userId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Permissions.Should().HaveCount(4);
        result.Value.Permissions.Should().Contain("users.read");
        result.Value.Permissions.Should().Contain("users.write");
        result.Value.Permissions.Should().Contain("content.read");
        result.Value.Permissions.Should().Contain("content.write");
    }

    [Fact]
    public async Task Handle_WithOverlappingPermissions_ShouldDeduplicatePermissions()
    {
        // Arrange
        const string userId = "user-123";
        var roles = new List<string> { "Admin", "Manager" };

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(userId));

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);

        _roleIdentityServiceMock
            .Setup(x => x.FindByNameAsync("Admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRoleDto("role-admin", "Admin"));

        _roleIdentityServiceMock
            .Setup(x => x.FindByNameAsync("Manager", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRoleDto("role-manager", "Manager"));

        _roleIdentityServiceMock
            .Setup(x => x.GetPermissionsAsync("role-admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "users.read", "users.write", "reports.read" });

        _roleIdentityServiceMock
            .Setup(x => x.GetPermissionsAsync("role-manager", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "users.read", "reports.read", "reports.write" }); // users.read and reports.read are duplicates

        var query = new GetUserPermissionsQuery(userId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Permissions.Should().HaveCount(4); // Deduplicated
        result.Value.Permissions.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task Handle_ShouldReturnUserEmailAndRoles()
    {
        // Arrange
        const string userId = "user-123";
        const string email = "admin@example.com";
        var roles = new List<string> { "Admin", "Editor" };

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(userId, email));

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);

        _roleIdentityServiceMock
            .Setup(x => x.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string name, CancellationToken _) => CreateTestRoleDto($"role-{name.ToLower()}", name));

        _roleIdentityServiceMock
            .Setup(x => x.GetPermissionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "some.permission" });

        var query = new GetUserPermissionsQuery(userId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(userId);
        result.Value.Email.Should().Be(email);
        result.Value.Roles.Should().BeEquivalentTo(roles);
    }

    [Fact]
    public async Task Handle_ShouldReturnPermissionsInSortedOrder()
    {
        // Arrange
        const string userId = "user-123";

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(userId));

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "Role" });

        _roleIdentityServiceMock
            .Setup(x => x.FindByNameAsync("Role", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRoleDto("role-id", "Role"));

        _roleIdentityServiceMock
            .Setup(x => x.GetPermissionsAsync("role-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "zebra.read", "apple.write", "banana.delete" });

        var query = new GetUserPermissionsQuery(userId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Permissions.Should().BeInAscendingOrder();
    }

    #endregion

    #region Empty Results

    [Fact]
    public async Task Handle_WhenUserHasNoRoles_ShouldReturnEmptyPermissions()
    {
        // Arrange
        const string userId = "user-no-roles";

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(userId));

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        var query = new GetUserPermissionsQuery(userId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Permissions.Should().BeEmpty();
        result.Value.Roles.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenRolesHaveNoPermissions_ShouldReturnEmptyPermissions()
    {
        // Arrange
        const string userId = "user-123";

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(userId));

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "EmptyRole" });

        _roleIdentityServiceMock
            .Setup(x => x.FindByNameAsync("EmptyRole", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRoleDto("role-empty", "EmptyRole"));

        _roleIdentityServiceMock
            .Setup(x => x.GetPermissionsAsync("role-empty", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        var query = new GetUserPermissionsQuery(userId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Permissions.Should().BeEmpty();
        result.Value.Roles.Should().ContainSingle().Which.Should().Be("EmptyRole");
    }

    #endregion

    #region Not Found Scenarios

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        const string userId = "non-existent-user";

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentityDto?)null);

        var query = new GetUserPermissionsQuery(userId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Auth.UserNotFound);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldNotAttemptToGetRoles()
    {
        // Arrange
        const string userId = "non-existent-user";

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentityDto?)null);

        var query = new GetUserPermissionsQuery(userId);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _userIdentityServiceMock.Verify(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _userIdentityServiceMock.Verify(x => x.GetRolesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRoleNotFoundByName_ShouldSkipThatRole()
    {
        // Arrange
        const string userId = "user-123";
        var roles = new List<string> { "ExistingRole", "DeletedRole" };

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(userId));

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);

        _roleIdentityServiceMock
            .Setup(x => x.FindByNameAsync("ExistingRole", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRoleDto("role-existing", "ExistingRole"));

        _roleIdentityServiceMock
            .Setup(x => x.FindByNameAsync("DeletedRole", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoleIdentityDto?)null); // Role was deleted

        _roleIdentityServiceMock
            .Setup(x => x.GetPermissionsAsync("role-existing", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "valid.permission" });

        var query = new GetUserPermissionsQuery(userId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Permissions.Should().ContainSingle().Which.Should().Be("valid.permission");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToAllServices()
    {
        // Arrange
        const string userId = "user-123";
        const string roleName = "TestRole";
        const string roleId = "role-123";

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(userId));

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { roleName });

        _roleIdentityServiceMock
            .Setup(x => x.FindByNameAsync(roleName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRoleDto(roleId, roleName));

        _roleIdentityServiceMock
            .Setup(x => x.GetPermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        var query = new GetUserPermissionsQuery(userId);
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await _handler.Handle(query, token);

        // Assert
        _userIdentityServiceMock.Verify(x => x.FindByIdAsync(userId, token), Times.Once);
        _userIdentityServiceMock.Verify(x => x.GetRolesAsync(userId, token), Times.Once);
        _roleIdentityServiceMock.Verify(x => x.FindByNameAsync(roleName, token), Times.Once);
        _roleIdentityServiceMock.Verify(x => x.GetPermissionsAsync(roleId, token), Times.Once);
    }

    [Fact]
    public async Task Handle_WithCaseInsensitivePermissions_ShouldDeduplicateCaseInsensitively()
    {
        // Arrange
        const string userId = "user-123";
        var roles = new List<string> { "Role1", "Role2" };

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(userId));

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);

        _roleIdentityServiceMock
            .Setup(x => x.FindByNameAsync("Role1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRoleDto("role-1", "Role1"));

        _roleIdentityServiceMock
            .Setup(x => x.FindByNameAsync("Role2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRoleDto("role-2", "Role2"));

        _roleIdentityServiceMock
            .Setup(x => x.GetPermissionsAsync("role-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "users.Read" });

        _roleIdentityServiceMock
            .Setup(x => x.GetPermissionsAsync("role-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "USERS.READ" }); // Same permission, different case

        var query = new GetUserPermissionsQuery(userId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // HashSet with OrdinalIgnoreCase should deduplicate
        result.Value.Permissions.Should().ContainSingle();
    }

    #endregion
}
