namespace NOIR.Application.UnitTests.Features.Roles;

/// <summary>
/// Unit tests for GetRoleByIdQueryHandler.
/// Tests single role retrieval scenarios with mocked dependencies.
/// </summary>
public class GetRoleByIdQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRoleIdentityService> _roleIdentityServiceMock;
    private readonly Mock<ILocalizationService> _localizationServiceMock;
    private readonly GetRoleByIdQueryHandler _handler;

    public GetRoleByIdQueryHandlerTests()
    {
        _roleIdentityServiceMock = new Mock<IRoleIdentityService>();
        _localizationServiceMock = new Mock<ILocalizationService>();

        // Setup localization to return the key (pass-through for testing)
        _localizationServiceMock
            .Setup(x => x[It.IsAny<string>()])
            .Returns<string>(key => key);

        _handler = new GetRoleByIdQueryHandler(
            _roleIdentityServiceMock.Object,
            _localizationServiceMock.Object);
    }

    private static RoleIdentityDto CreateTestRoleDto(
        string id = "test-role-id",
        string name = "TestRole",
        string? normalizedName = "TESTROLE",
        string? description = "Test Description",
        string? parentRoleId = null,
        Guid? tenantId = null,
        bool isSystemRole = false,
        bool isPlatformRole = false,
        int sortOrder = 0,
        string? iconName = null,
        string? color = null)
    {
        return new RoleIdentityDto(
            id,
            name,
            normalizedName,
            description,
            parentRoleId,
            tenantId,
            isSystemRole,
            isPlatformRole,
            sortOrder,
            iconName,
            color);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidId_ShouldReturnRole()
    {
        // Arrange
        var roleId = "role-123";
        var role = CreateTestRoleDto(
            id: roleId,
            name: "Administrator",
            description: "Admin role with full access");
        var permissions = new List<string> { "users.read", "users.write" };
        var effectivePermissions = new List<string> { "users.read", "users.write", "roles.read" };

        _roleIdentityServiceMock
            .Setup(x => x.FindByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        _roleIdentityServiceMock
            .Setup(x => x.GetPermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);

        _roleIdentityServiceMock
            .Setup(x => x.GetEffectivePermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(effectivePermissions);

        _roleIdentityServiceMock
            .Setup(x => x.GetUserCountAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var query = new GetRoleByIdQuery(roleId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Id.ShouldBe(roleId);
        result.Value.Name.ShouldBe("Administrator");
        result.Value.Description.ShouldBe("Admin role with full access");
        result.Value.UserCount.ShouldBe(5);
        result.Value.Permissions.ShouldBe(permissions);
        result.Value.EffectivePermissions.ShouldBe(effectivePermissions);
    }

    [Fact]
    public async Task Handle_WithSystemRole_ShouldReturnSystemRoleFlag()
    {
        // Arrange
        var roleId = "system-role-id";
        var role = CreateTestRoleDto(
            id: roleId,
            name: "SuperAdmin",
            isSystemRole: true);

        _roleIdentityServiceMock
            .Setup(x => x.FindByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        _roleIdentityServiceMock
            .Setup(x => x.GetPermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        _roleIdentityServiceMock
            .Setup(x => x.GetEffectivePermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        _roleIdentityServiceMock
            .Setup(x => x.GetUserCountAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var query = new GetRoleByIdQuery(roleId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.IsSystemRole.ShouldBe(true);
    }

    [Fact]
    public async Task Handle_WithParentRole_ShouldIncludeParentRoleName()
    {
        // Arrange
        var roleId = "child-role-id";
        var parentRoleId = "parent-role-id";
        var childRole = CreateTestRoleDto(
            id: roleId,
            name: "Manager",
            parentRoleId: parentRoleId);
        var parentRole = CreateTestRoleDto(
            id: parentRoleId,
            name: "Administrator");

        _roleIdentityServiceMock
            .Setup(x => x.FindByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(childRole);

        _roleIdentityServiceMock
            .Setup(x => x.FindByIdAsync(parentRoleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentRole);

        _roleIdentityServiceMock
            .Setup(x => x.GetPermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        _roleIdentityServiceMock
            .Setup(x => x.GetEffectivePermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        _roleIdentityServiceMock
            .Setup(x => x.GetUserCountAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetRoleByIdQuery(roleId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ParentRoleId.ShouldBe(parentRoleId);
        result.Value.ParentRoleName.ShouldBe("Administrator");
    }

    [Fact]
    public async Task Handle_WithoutParentRole_ShouldReturnNullParentName()
    {
        // Arrange
        var roleId = "standalone-role-id";
        var role = CreateTestRoleDto(
            id: roleId,
            name: "StandaloneRole",
            parentRoleId: null);

        _roleIdentityServiceMock
            .Setup(x => x.FindByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        _roleIdentityServiceMock
            .Setup(x => x.GetPermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        _roleIdentityServiceMock
            .Setup(x => x.GetEffectivePermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        _roleIdentityServiceMock
            .Setup(x => x.GetUserCountAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetRoleByIdQuery(roleId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ParentRoleId.ShouldBeNull();
        result.Value.ParentRoleName.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_WithTenantId_ShouldIncludeTenantId()
    {
        // Arrange
        var roleId = "tenant-role-id";
        var tenantId = Guid.NewGuid();
        var role = CreateTestRoleDto(
            id: roleId,
            name: "TenantAdmin",
            tenantId: tenantId);

        _roleIdentityServiceMock
            .Setup(x => x.FindByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        _roleIdentityServiceMock
            .Setup(x => x.GetPermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        _roleIdentityServiceMock
            .Setup(x => x.GetEffectivePermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        _roleIdentityServiceMock
            .Setup(x => x.GetUserCountAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetRoleByIdQuery(roleId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.TenantId.ShouldBe(tenantId);
    }

    [Fact]
    public async Task Handle_ShouldMapAllFieldsCorrectly()
    {
        // Arrange
        var roleId = "full-role-id";
        var tenantId = Guid.NewGuid();
        var role = CreateTestRoleDto(
            id: roleId,
            name: "FullRole",
            normalizedName: "FULLROLE",
            description: "Full description",
            parentRoleId: null,
            tenantId: tenantId,
            isSystemRole: false,
            sortOrder: 10,
            iconName: "shield",
            color: "#FF5733");

        _roleIdentityServiceMock
            .Setup(x => x.FindByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        _roleIdentityServiceMock
            .Setup(x => x.GetPermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "perm1" });

        _roleIdentityServiceMock
            .Setup(x => x.GetEffectivePermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "perm1", "perm2" });

        _roleIdentityServiceMock
            .Setup(x => x.GetUserCountAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(25);

        var query = new GetRoleByIdQuery(roleId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        var dto = result.Value;
        dto.Id.ShouldBe(roleId);
        dto.Name.ShouldBe("FullRole");
        dto.NormalizedName.ShouldBe("FULLROLE");
        dto.Description.ShouldBe("Full description");
        dto.TenantId.ShouldBe(tenantId);
        dto.IsSystemRole.ShouldBe(false);
        dto.SortOrder.ShouldBe(10);
        dto.IconName.ShouldBe("shield");
        dto.Color.ShouldBe("#FF5733");
        dto.UserCount.ShouldBe(25);
        dto.Permissions.Count().ShouldBe(1);
        dto.EffectivePermissions.Count().ShouldBe(2);
    }

    #endregion

    #region Not Found Scenarios

    [Fact]
    public async Task Handle_WhenRoleNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var roleId = "non-existent-role";

        _roleIdentityServiceMock
            .Setup(x => x.FindByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoleIdentityDto?)null);

        var query = new GetRoleByIdQuery(roleId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Code.ShouldBe(ErrorCodes.Auth.RoleNotFound);
    }

    [Fact]
    public async Task Handle_WhenRoleNotFound_ShouldNotCallOtherServices()
    {
        // Arrange
        var roleId = "non-existent-role";

        _roleIdentityServiceMock
            .Setup(x => x.FindByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoleIdentityDto?)null);

        var query = new GetRoleByIdQuery(roleId);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _roleIdentityServiceMock.Verify(
            x => x.GetPermissionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _roleIdentityServiceMock.Verify(
            x => x.GetEffectivePermissionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _roleIdentityServiceMock.Verify(
            x => x.GetUserCountAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region CancellationToken Propagation

    [Fact]
    public async Task Handle_ShouldPropagateCancellationToken()
    {
        // Arrange
        var roleId = "role-123";
        var role = CreateTestRoleDto(id: roleId, name: "TestRole");
        var cancellationToken = new CancellationToken(false);

        _roleIdentityServiceMock
            .Setup(x => x.FindByIdAsync(roleId, cancellationToken))
            .ReturnsAsync(role);

        _roleIdentityServiceMock
            .Setup(x => x.GetPermissionsAsync(roleId, cancellationToken))
            .ReturnsAsync(new List<string>());

        _roleIdentityServiceMock
            .Setup(x => x.GetEffectivePermissionsAsync(roleId, cancellationToken))
            .ReturnsAsync(new List<string>());

        _roleIdentityServiceMock
            .Setup(x => x.GetUserCountAsync(roleId, cancellationToken))
            .ReturnsAsync(0);

        var query = new GetRoleByIdQuery(roleId);

        // Act
        await _handler.Handle(query, cancellationToken);

        // Assert
        _roleIdentityServiceMock.Verify(
            x => x.FindByIdAsync(roleId, cancellationToken),
            Times.Once);
        _roleIdentityServiceMock.Verify(
            x => x.GetPermissionsAsync(roleId, cancellationToken),
            Times.Once);
        _roleIdentityServiceMock.Verify(
            x => x.GetEffectivePermissionsAsync(roleId, cancellationToken),
            Times.Once);
        _roleIdentityServiceMock.Verify(
            x => x.GetUserCountAsync(roleId, cancellationToken),
            Times.Once);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithEmptyPermissions_ShouldReturnEmptyLists()
    {
        // Arrange
        var roleId = "role-no-perms";
        var role = CreateTestRoleDto(id: roleId, name: "NoPermsRole");

        _roleIdentityServiceMock
            .Setup(x => x.FindByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        _roleIdentityServiceMock
            .Setup(x => x.GetPermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        _roleIdentityServiceMock
            .Setup(x => x.GetEffectivePermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        _roleIdentityServiceMock
            .Setup(x => x.GetUserCountAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetRoleByIdQuery(roleId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Permissions.ShouldBeEmpty();
        result.Value.EffectivePermissions.ShouldBeEmpty();
        result.Value.UserCount.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_WhenParentRoleDeletedOrNotFound_ShouldReturnNullParentName()
    {
        // Arrange
        var roleId = "child-role-id";
        var parentRoleId = "deleted-parent-id";
        var childRole = CreateTestRoleDto(
            id: roleId,
            name: "OrphanRole",
            parentRoleId: parentRoleId);

        _roleIdentityServiceMock
            .Setup(x => x.FindByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(childRole);

        _roleIdentityServiceMock
            .Setup(x => x.FindByIdAsync(parentRoleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoleIdentityDto?)null);

        _roleIdentityServiceMock
            .Setup(x => x.GetPermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        _roleIdentityServiceMock
            .Setup(x => x.GetEffectivePermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        _roleIdentityServiceMock
            .Setup(x => x.GetUserCountAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetRoleByIdQuery(roleId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ParentRoleId.ShouldBe(parentRoleId);
        result.Value.ParentRoleName.ShouldBeNull();
    }

    #endregion
}
