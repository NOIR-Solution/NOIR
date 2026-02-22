namespace NOIR.Application.UnitTests.Features.Roles;

/// <summary>
/// Unit tests for GetRolesQueryHandler.
/// Tests paginated role list retrieval scenarios with filtering.
/// </summary>
public class GetRolesQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRoleIdentityService> _roleIdentityServiceMock;
    private readonly GetRolesQueryHandler _handler;

    public GetRolesQueryHandlerTests()
    {
        _roleIdentityServiceMock = new Mock<IRoleIdentityService>();

        // Default: return empty permission counts (tests that need specific values will override)
        _roleIdentityServiceMock
            .Setup(x => x.GetPermissionCountsAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, int>() as IReadOnlyDictionary<string, int>);

        _handler = new GetRolesQueryHandler(_roleIdentityServiceMock.Object);
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
    public async Task Handle_WithNoFilters_ShouldReturnAllRoles()
    {
        // Arrange
        var roles = new List<RoleIdentityDto>
        {
            CreateTestRoleDto(id: "role-1", name: "Admin"),
            CreateTestRoleDto(id: "role-2", name: "Manager"),
            CreateTestRoleDto(id: "role-3", name: "User")
        };

        var userCounts = new Dictionary<string, int>
        {
            { "role-1", 5 },
            { "role-2", 10 },
            { "role-3", 100 }
        };

        _roleIdentityServiceMock
            .Setup(x => x.GetRolesPaginatedAsync(
                null, 1, 20, null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((roles as IReadOnlyList<RoleIdentityDto>, 3));

        _roleIdentityServiceMock
            .Setup(x => x.GetUserCountsAsync(
                It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "role-1", "role-2", "role-3" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(userCounts as IReadOnlyDictionary<string, int>);

        var query = new GetRolesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        result.Value.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task Handle_WithSearchFilter_ShouldFilterRoles()
    {
        // Arrange
        var roles = new List<RoleIdentityDto>
        {
            CreateTestRoleDto(id: "role-1", name: "Admin")
        };

        var userCounts = new Dictionary<string, int> { { "role-1", 5 } };

        _roleIdentityServiceMock
            .Setup(x => x.GetRolesPaginatedAsync(
                "admin", 1, 20, null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((roles as IReadOnlyList<RoleIdentityDto>, 1));

        _roleIdentityServiceMock
            .Setup(x => x.GetUserCountsAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(userCounts as IReadOnlyDictionary<string, int>);

        var query = new GetRolesQuery(Search: "admin");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].Name.Should().Be("Admin");
    }

    [Fact]
    public async Task Handle_WithTenantFilter_ShouldFilterByTenant()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var roles = new List<RoleIdentityDto>
        {
            CreateTestRoleDto(id: "role-1", name: "TenantAdmin", tenantId: tenantId),
            CreateTestRoleDto(id: "role-2", name: "SuperAdmin", isSystemRole: true)
        };

        var userCounts = new Dictionary<string, int> { { "role-1", 3 }, { "role-2", 1 } };

        _roleIdentityServiceMock
            .Setup(x => x.GetRolesPaginatedAsync(
                null, 1, 20, tenantId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((roles as IReadOnlyList<RoleIdentityDto>, 2));

        _roleIdentityServiceMock
            .Setup(x => x.GetUserCountsAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(userCounts as IReadOnlyDictionary<string, int>);

        var query = new GetRolesQuery(TenantId: tenantId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ExcludingSystemRoles_ShouldFilterOutSystemRoles()
    {
        // Arrange
        var roles = new List<RoleIdentityDto>
        {
            CreateTestRoleDto(id: "role-1", name: "CustomRole", isSystemRole: false)
        };

        var userCounts = new Dictionary<string, int> { { "role-1", 5 } };

        _roleIdentityServiceMock
            .Setup(x => x.GetRolesPaginatedAsync(
                null, 1, 20, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((roles as IReadOnlyList<RoleIdentityDto>, 1));

        _roleIdentityServiceMock
            .Setup(x => x.GetUserCountsAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(userCounts as IReadOnlyDictionary<string, int>);

        var query = new GetRolesQuery(IncludeSystemRoles: false);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].IsSystemRole.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldMapAllFieldsCorrectly()
    {
        // Arrange
        var roles = new List<RoleIdentityDto>
        {
            CreateTestRoleDto(
                id: "role-1",
                name: "FullRole",
                description: "Full description",
                parentRoleId: "parent-role",
                isSystemRole: true,
                sortOrder: 10,
                iconName: "shield",
                color: "#FF5733")
        };

        var userCounts = new Dictionary<string, int> { { "role-1", 25 } };
        var permissionCounts = new Dictionary<string, int> { { "role-1", 12 } };

        _roleIdentityServiceMock
            .Setup(x => x.GetRolesPaginatedAsync(
                null, 1, 20, null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((roles as IReadOnlyList<RoleIdentityDto>, 1));

        _roleIdentityServiceMock
            .Setup(x => x.GetUserCountsAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(userCounts as IReadOnlyDictionary<string, int>);

        _roleIdentityServiceMock
            .Setup(x => x.GetPermissionCountsAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissionCounts as IReadOnlyDictionary<string, int>);

        var query = new GetRolesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value.Items[0];
        dto.Id.Should().Be("role-1");
        dto.Name.Should().Be("FullRole");
        dto.Description.Should().Be("Full description");
        dto.ParentRoleId.Should().Be("parent-role");
        dto.IsSystemRole.Should().BeTrue();
        dto.SortOrder.Should().Be(10);
        dto.IconName.Should().Be("shield");
        dto.Color.Should().Be("#FF5733");
        dto.UserCount.Should().Be(25);
        dto.PermissionCount.Should().Be(12);
    }

    [Fact]
    public async Task Handle_WithMissingUserCount_ShouldReturnZero()
    {
        // Arrange
        var roles = new List<RoleIdentityDto>
        {
            CreateTestRoleDto(id: "role-1", name: "RoleWithNoUsers")
        };

        // Empty user counts - role not in dictionary
        var userCounts = new Dictionary<string, int>();

        _roleIdentityServiceMock
            .Setup(x => x.GetRolesPaginatedAsync(
                null, 1, 20, null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((roles as IReadOnlyList<RoleIdentityDto>, 1));

        _roleIdentityServiceMock
            .Setup(x => x.GetUserCountsAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(userCounts as IReadOnlyDictionary<string, int>);

        var query = new GetRolesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items[0].UserCount.Should().Be(0);
    }

    #endregion

    #region Pagination Scenarios

    [Fact]
    public async Task Handle_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var roles = new List<RoleIdentityDto>
        {
            CreateTestRoleDto(id: "role-6", name: "Role6"),
            CreateTestRoleDto(id: "role-7", name: "Role7"),
            CreateTestRoleDto(id: "role-8", name: "Role8"),
            CreateTestRoleDto(id: "role-9", name: "Role9"),
            CreateTestRoleDto(id: "role-10", name: "Role10")
        };

        var userCounts = roles.ToDictionary(r => r.Id, _ => 1);

        _roleIdentityServiceMock
            .Setup(x => x.GetRolesPaginatedAsync(
                null, 2, 5, null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((roles as IReadOnlyList<RoleIdentityDto>, 25));

        _roleIdentityServiceMock
            .Setup(x => x.GetUserCountsAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(userCounts as IReadOnlyDictionary<string, int>);

        var query = new GetRolesQuery(Page: 2, PageSize: 5);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        result.Value.TotalCount.Should().Be(25);
        result.Value.PageNumber.Should().Be(2);
        result.Value.TotalPages.Should().Be(5);
    }

    [Fact]
    public async Task Handle_WithCustomPageSize_ShouldReturnCorrectCount()
    {
        // Arrange
        var roles = new List<RoleIdentityDto>
        {
            CreateTestRoleDto(id: "role-1", name: "Role1"),
            CreateTestRoleDto(id: "role-2", name: "Role2")
        };

        var userCounts = roles.ToDictionary(r => r.Id, _ => 1);

        _roleIdentityServiceMock
            .Setup(x => x.GetRolesPaginatedAsync(
                null, 1, 2, null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((roles as IReadOnlyList<RoleIdentityDto>, 10));

        _roleIdentityServiceMock
            .Setup(x => x.GetUserCountsAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(userCounts as IReadOnlyDictionary<string, int>);

        var query = new GetRolesQuery(PageSize: 2);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(10);
        result.Value.TotalPages.Should().Be(5);
    }

    [Fact]
    public async Task Handle_WithLastPage_ShouldReturnRemainingItems()
    {
        // Arrange
        var roles = new List<RoleIdentityDto>
        {
            CreateTestRoleDto(id: "role-9", name: "Role9"),
            CreateTestRoleDto(id: "role-10", name: "Role10")
        };

        var userCounts = roles.ToDictionary(r => r.Id, _ => 1);

        _roleIdentityServiceMock
            .Setup(x => x.GetRolesPaginatedAsync(
                null, 2, 5, null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((roles as IReadOnlyList<RoleIdentityDto>, 7));

        _roleIdentityServiceMock
            .Setup(x => x.GetUserCountsAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(userCounts as IReadOnlyDictionary<string, int>);

        var query = new GetRolesQuery(Page: 2, PageSize: 5);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(7);
        result.Value.HasNextPage.Should().BeFalse();
    }

    #endregion

    #region Empty Results Scenarios

    [Fact]
    public async Task Handle_WithNoRoles_ShouldReturnEmptyList()
    {
        // Arrange
        var roles = new List<RoleIdentityDto>();
        var userCounts = new Dictionary<string, int>();

        _roleIdentityServiceMock
            .Setup(x => x.GetRolesPaginatedAsync(
                null, 1, 20, null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((roles as IReadOnlyList<RoleIdentityDto>, 0));

        _roleIdentityServiceMock
            .Setup(x => x.GetUserCountsAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(userCounts as IReadOnlyDictionary<string, int>);

        var query = new GetRolesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithSearchNoMatch_ShouldReturnEmptyList()
    {
        // Arrange
        var roles = new List<RoleIdentityDto>();
        var userCounts = new Dictionary<string, int>();

        _roleIdentityServiceMock
            .Setup(x => x.GetRolesPaginatedAsync(
                "nonexistent", 1, 20, null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((roles as IReadOnlyList<RoleIdentityDto>, 0));

        _roleIdentityServiceMock
            .Setup(x => x.GetUserCountsAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(userCounts as IReadOnlyDictionary<string, int>);

        var query = new GetRolesQuery(Search: "nonexistent");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    #endregion

    #region CancellationToken Propagation

    [Fact]
    public async Task Handle_ShouldPropagateCancellationToken()
    {
        // Arrange
        var cancellationToken = new CancellationToken(false);
        var roles = new List<RoleIdentityDto>
        {
            CreateTestRoleDto(id: "role-1", name: "TestRole")
        };
        var userCounts = new Dictionary<string, int> { { "role-1", 1 } };

        _roleIdentityServiceMock
            .Setup(x => x.GetRolesPaginatedAsync(
                null, 1, 20, null, true, cancellationToken))
            .ReturnsAsync((roles as IReadOnlyList<RoleIdentityDto>, 1));

        _roleIdentityServiceMock
            .Setup(x => x.GetUserCountsAsync(
                It.IsAny<IEnumerable<string>>(),
                cancellationToken))
            .ReturnsAsync(userCounts as IReadOnlyDictionary<string, int>);

        var query = new GetRolesQuery();

        // Act
        await _handler.Handle(query, cancellationToken);

        // Assert
        _roleIdentityServiceMock.Verify(
            x => x.GetRolesPaginatedAsync(null, 1, 20, null, true, cancellationToken),
            Times.Once);
        _roleIdentityServiceMock.Verify(
            x => x.GetUserCountsAsync(It.IsAny<IEnumerable<string>>(), cancellationToken),
            Times.Once);
    }

    #endregion

    #region Combined Filters Scenarios

    [Fact]
    public async Task Handle_WithSearchAndTenantFilter_ShouldApplyBothFilters()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var roles = new List<RoleIdentityDto>
        {
            CreateTestRoleDto(id: "role-1", name: "TenantAdmin", tenantId: tenantId)
        };
        var userCounts = new Dictionary<string, int> { { "role-1", 3 } };

        _roleIdentityServiceMock
            .Setup(x => x.GetRolesPaginatedAsync(
                "admin", 1, 20, tenantId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((roles as IReadOnlyList<RoleIdentityDto>, 1));

        _roleIdentityServiceMock
            .Setup(x => x.GetUserCountsAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(userCounts as IReadOnlyDictionary<string, int>);

        var query = new GetRolesQuery(Search: "admin", TenantId: tenantId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);

        _roleIdentityServiceMock.Verify(
            x => x.GetRolesPaginatedAsync("admin", 1, 20, tenantId, true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithAllFilters_ShouldApplyAllFilters()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var roles = new List<RoleIdentityDto>
        {
            CreateTestRoleDto(id: "role-1", name: "Manager", tenantId: tenantId, isSystemRole: false)
        };
        var userCounts = new Dictionary<string, int> { { "role-1", 5 } };

        _roleIdentityServiceMock
            .Setup(x => x.GetRolesPaginatedAsync(
                "manager", 2, 10, tenantId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((roles as IReadOnlyList<RoleIdentityDto>, 1));

        _roleIdentityServiceMock
            .Setup(x => x.GetUserCountsAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(userCounts as IReadOnlyDictionary<string, int>);

        var query = new GetRolesQuery(
            Search: "manager",
            Page: 2,
            PageSize: 10,
            TenantId: tenantId,
            IncludeSystemRoles: false);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _roleIdentityServiceMock.Verify(
            x => x.GetRolesPaginatedAsync("manager", 2, 10, tenantId, false, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
