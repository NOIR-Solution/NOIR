namespace NOIR.Application.UnitTests.Features.Users;

/// <summary>
/// Unit tests for GetUsersQueryHandler.
/// Tests user listing with search, filtering, and pagination.
/// </summary>
public class GetUsersQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IUserIdentityService> _userIdentityServiceMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly GetUsersQueryHandler _handler;
    private const string TestTenantId = "test-tenant-id";

    public GetUsersQueryHandlerTests()
    {
        _userIdentityServiceMock = new Mock<IUserIdentityService>();
        _currentUserMock = new Mock<ICurrentUser>();

        // Setup current user with test tenant
        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);

        _handler = new GetUsersQueryHandler(
            _userIdentityServiceMock.Object,
            _currentUserMock.Object);
    }

    private static UserIdentityDto CreateTestUserDto(
        string id,
        string email,
        string? firstName = "Test",
        string? lastName = "User",
        string? displayName = null,
        bool isActive = true,
        bool isSystemUser = false)
    {
        return new UserIdentityDto(
            Id: id,
            Email: email,
            TenantId: "default",
            FirstName: firstName,
            LastName: lastName,
            DisplayName: displayName,
            FullName: $"{firstName} {lastName}".Trim(),
            PhoneNumber: null,
            AvatarUrl: null,
            IsActive: isActive,
            IsDeleted: false,
            IsSystemUser: isSystemUser,
            CreatedAt: DateTimeOffset.UtcNow,
            ModifiedAt: null);
    }

    private static List<UserIdentityDto> CreateTestUsers(int count)
    {
        var users = new List<UserIdentityDto>();
        for (var i = 1; i <= count; i++)
        {
            users.Add(CreateTestUserDto(
                id: $"user-{i}",
                email: $"user{i}@example.com",
                firstName: $"User{i}",
                lastName: $"Last{i}",
                isActive: i % 2 == 0)); // Even users are active, odd are locked
        }
        return users;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithNoFilters_ShouldReturnAllUsers()
    {
        // Arrange
        var users = CreateTestUsers(5);
        var userRoles = new Dictionary<string, List<string>>
        {
            { "user-1", new List<string> { "Admin" } },
            { "user-2", new List<string> { "User" } },
            { "user-3", new List<string> { "Admin", "User" } },
            { "user-4", new List<string>() },
            { "user-5", new List<string> { "Manager" } }
        };

        _userIdentityServiceMock
            .Setup(x => x.GetUsersPaginatedAsync(TestTenantId, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((users, users.Count));

        foreach (var user in users)
        {
            _userIdentityServiceMock
                .Setup(x => x.GetRolesAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(userRoles[user.Id]);
        }

        var query = new GetUsersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        result.Value.TotalCount.Should().Be(5);
        result.Value.PageNumber.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithSearchFilter_ShouldPassSearchToService()
    {
        // Arrange
        const string searchTerm = "john";
        var users = new List<UserIdentityDto>
        {
            CreateTestUserDto("user-1", "john@example.com", "John", "Doe")
        };

        _userIdentityServiceMock
            .Setup(x => x.GetUsersPaginatedAsync(TestTenantId, searchTerm, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((users, users.Count));

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "User" });

        var query = new GetUsersQuery(Search: searchTerm);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        _userIdentityServiceMock.Verify(
            x => x.GetUsersPaginatedAsync(TestTenantId, searchTerm, 1, 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithRoleFilter_ShouldFilterByRole()
    {
        // Arrange
        var users = CreateTestUsers(3);

        _userIdentityServiceMock
            .Setup(x => x.GetUsersPaginatedAsync(TestTenantId, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((users, users.Count));

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "Admin" });

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync("user-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "User" });

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync("user-3", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "Admin", "User" });

        var query = new GetUsersQuery(Role: "Admin");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Only users with Admin role should be included
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items.All(u => u.Roles.Contains("Admin")).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithIsLockedFilter_ShouldFilterByLockStatus()
    {
        // Arrange
        var users = new List<UserIdentityDto>
        {
            CreateTestUserDto("user-1", "active@example.com", isActive: true),
            CreateTestUserDto("user-2", "locked@example.com", isActive: false),
            CreateTestUserDto("user-3", "another-active@example.com", isActive: true)
        };

        _userIdentityServiceMock
            .Setup(x => x.GetUsersPaginatedAsync(TestTenantId, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((users, users.Count));

        foreach (var user in users)
        {
            _userIdentityServiceMock
                .Setup(x => x.GetRolesAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<string>());
        }

        var query = new GetUsersQuery(IsLocked: true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Only locked users (IsActive = false means IsLocked = true)
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items.Should().OnlyContain(u => u.IsLocked);
    }

    [Fact]
    public async Task Handle_WithIsLockedFalseFilter_ShouldReturnActiveUsers()
    {
        // Arrange
        var users = new List<UserIdentityDto>
        {
            CreateTestUserDto("user-1", "active@example.com", isActive: true),
            CreateTestUserDto("user-2", "locked@example.com", isActive: false),
            CreateTestUserDto("user-3", "another-active@example.com", isActive: true)
        };

        _userIdentityServiceMock
            .Setup(x => x.GetUsersPaginatedAsync(TestTenantId, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((users, users.Count));

        foreach (var user in users)
        {
            _userIdentityServiceMock
                .Setup(x => x.GetRolesAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<string>());
        }

        var query = new GetUsersQuery(IsLocked: false);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Only active users (IsLocked = false)
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items.Should().OnlyContain(u => !u.IsLocked);
    }

    #endregion

    #region Pagination Scenarios

    [Fact]
    public async Task Handle_WithCustomPageSize_ShouldPassPageSizeToService()
    {
        // Arrange
        const int pageSize = 10;
        var users = CreateTestUsers(10);

        _userIdentityServiceMock
            .Setup(x => x.GetUsersPaginatedAsync(TestTenantId, null, 1, pageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync((users, 25)); // Total count is 25

        foreach (var user in users)
        {
            _userIdentityServiceMock
                .Setup(x => x.GetRolesAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<string>());
        }

        var query = new GetUsersQuery(PageSize: pageSize);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(10);
        result.Value.TotalCount.Should().Be(25);
        result.Value.TotalPages.Should().Be(3); // 25 / 10 = 3 pages (rounded up)
    }

    [Fact]
    public async Task Handle_WithPage2_ShouldPassPageToService()
    {
        // Arrange
        const int page = 2;
        const int pageSize = 5;
        var users = CreateTestUsers(5);

        _userIdentityServiceMock
            .Setup(x => x.GetUsersPaginatedAsync(TestTenantId, null, page, pageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync((users, 15)); // Total count is 15

        foreach (var user in users)
        {
            _userIdentityServiceMock
                .Setup(x => x.GetRolesAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<string>());
        }

        var query = new GetUsersQuery(Page: page, PageSize: pageSize);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PageNumber.Should().Be(2);
        _userIdentityServiceMock.Verify(
            x => x.GetUsersPaginatedAsync(TestTenantId, null, page, pageSize, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyResult_ShouldReturnEmptyPaginatedList()
    {
        // Arrange
        _userIdentityServiceMock
            .Setup(x => x.GetUsersPaginatedAsync(TestTenantId, It.IsAny<string?>(), 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<UserIdentityDto>(), 0));

        var query = new GetUsersQuery(Search: "nonexistent");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
        result.Value.TotalPages.Should().Be(0);
    }

    #endregion

    #region Combined Filters Scenarios

    [Fact]
    public async Task Handle_WithRoleAndIsLockedFilter_ShouldApplyBothFilters()
    {
        // Arrange
        var users = new List<UserIdentityDto>
        {
            CreateTestUserDto("user-1", "admin-active@example.com", isActive: true),
            CreateTestUserDto("user-2", "admin-locked@example.com", isActive: false),
            CreateTestUserDto("user-3", "user-active@example.com", isActive: true),
            CreateTestUserDto("user-4", "user-locked@example.com", isActive: false)
        };

        _userIdentityServiceMock
            .Setup(x => x.GetUsersPaginatedAsync(TestTenantId, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((users, users.Count));

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "Admin" });
        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync("user-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "Admin" });
        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync("user-3", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "User" });
        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync("user-4", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "User" });

        // Filter: Admin role AND locked
        var query = new GetUsersQuery(Role: "Admin", IsLocked: true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Only user-2 matches both criteria (Admin AND locked)
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].Id.Should().Be("user-2");
    }

    [Fact]
    public async Task Handle_RoleFilterIsCaseInsensitive_ShouldMatchRegardlessOfCase()
    {
        // Arrange
        var users = new List<UserIdentityDto>
        {
            CreateTestUserDto("user-1", "admin@example.com")
        };

        _userIdentityServiceMock
            .Setup(x => x.GetUsersPaginatedAsync(TestTenantId, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((users, 1));

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "Admin" }); // Capitalized

        // Query with lowercase role
        var query = new GetUsersQuery(Role: "admin");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
    }

    #endregion

    #region UserListDto Mapping Scenarios

    [Fact]
    public async Task Handle_ShouldMapDisplayNameOrFullName()
    {
        // Arrange
        var users = new List<UserIdentityDto>
        {
            CreateTestUserDto("user-1", "withdisplay@example.com", displayName: "Custom Display"),
            CreateTestUserDto("user-2", "nodisplay@example.com", firstName: "John", lastName: "Doe", displayName: null)
        };

        _userIdentityServiceMock
            .Setup(x => x.GetUsersPaginatedAsync(TestTenantId, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((users, users.Count));

        foreach (var user in users)
        {
            _userIdentityServiceMock
                .Setup(x => x.GetRolesAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<string>());
        }

        var query = new GetUsersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items[0].DisplayName.Should().Be("Custom Display");
        result.Value.Items[1].DisplayName.Should().Be("John Doe"); // Falls back to FullName
    }

    [Fact]
    public async Task Handle_ShouldMapIsSystemUser()
    {
        // Arrange
        var users = new List<UserIdentityDto>
        {
            CreateTestUserDto("user-1", "regular@example.com", isSystemUser: false),
            CreateTestUserDto("user-2", "system@example.com", isSystemUser: true)
        };

        _userIdentityServiceMock
            .Setup(x => x.GetUsersPaginatedAsync(TestTenantId, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((users, users.Count));

        foreach (var user in users)
        {
            _userIdentityServiceMock
                .Setup(x => x.GetRolesAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<string>());
        }

        var query = new GetUsersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items[0].IsSystemUser.Should().BeFalse();
        result.Value.Items[1].IsSystemUser.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldMapIsLockedFromIsActive()
    {
        // Arrange
        var users = new List<UserIdentityDto>
        {
            CreateTestUserDto("user-1", "active@example.com", isActive: true),
            CreateTestUserDto("user-2", "locked@example.com", isActive: false)
        };

        _userIdentityServiceMock
            .Setup(x => x.GetUsersPaginatedAsync(TestTenantId, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((users, users.Count));

        foreach (var user in users)
        {
            _userIdentityServiceMock
                .Setup(x => x.GetRolesAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<string>());
        }

        var query = new GetUsersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // IsLocked is the inverse of IsActive
        result.Value.Items[0].IsLocked.Should().BeFalse(); // IsActive=true means IsLocked=false
        result.Value.Items[1].IsLocked.Should().BeTrue();  // IsActive=false means IsLocked=true
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToServices()
    {
        // Arrange
        var users = new List<UserIdentityDto>
        {
            CreateTestUserDto("user-1", "test@example.com")
        };

        _userIdentityServiceMock
            .Setup(x => x.GetUsersPaginatedAsync(TestTenantId, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((users, 1));

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        var query = new GetUsersQuery();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await _handler.Handle(query, token);

        // Assert
        _userIdentityServiceMock.Verify(
            x => x.GetUsersPaginatedAsync(TestTenantId, null, 1, 20, token),
            Times.Once);
        _userIdentityServiceMock.Verify(
            x => x.GetRolesAsync("user-1", token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DefaultValues_ShouldUsePage1AndPageSize20()
    {
        // Arrange
        _userIdentityServiceMock
            .Setup(x => x.GetUsersPaginatedAsync(TestTenantId, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<UserIdentityDto>(), 0));

        var query = new GetUsersQuery(); // Using all defaults

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _userIdentityServiceMock.Verify(
            x => x.GetUsersPaginatedAsync(TestTenantId, null, 1, 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_FilteringReducesItemsButTotalCountFromService_ShouldReflectOriginalCount()
    {
        // Arrange
        var users = CreateTestUsers(10);

        _userIdentityServiceMock
            .Setup(x => x.GetUsersPaginatedAsync(TestTenantId, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((users, 50)); // Service returns total of 50, but only 10 on this page

        foreach (var user in users)
        {
            _userIdentityServiceMock
                .Setup(x => x.GetRolesAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<string>()); // No roles - will be filtered out by role filter
        }

        var query = new GetUsersQuery(Role: "Admin"); // Filter by Admin role

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Items filtered down (no one has Admin role)
        result.Value.Items.Should().BeEmpty();
        // But TotalCount reflects the original service count (this is a known behavior)
        result.Value.TotalCount.Should().Be(50);
    }

    #endregion
}
