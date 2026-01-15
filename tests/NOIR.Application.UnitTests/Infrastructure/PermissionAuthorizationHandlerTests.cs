namespace NOIR.Application.UnitTests.Infrastructure;

/// <summary>
/// Unit tests for PermissionAuthorizationHandler.
/// Tests permission-based authorization with caching.
/// </summary>
public class PermissionAuthorizationHandlerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<RoleManager<ApplicationRole>> _roleManagerMock;
    private readonly IMemoryCache _cache;
    private readonly PermissionAuthorizationHandler _sut;

    public PermissionAuthorizationHandlerTests()
    {
        // Setup UserManager mock
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        // Setup RoleManager mock
        var roleStore = new Mock<IRoleStore<ApplicationRole>>();
        _roleManagerMock = new Mock<RoleManager<ApplicationRole>>(
            roleStore.Object, null!, null!, null!, null!);

        // Use real MemoryCache for testing
        _cache = new MemoryCache(new MemoryCacheOptions());

        _sut = new PermissionAuthorizationHandler(
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _cache);
    }

    private static ClaimsPrincipal CreateUser(string userId, bool isAuthenticated = true)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId)
        };
        var identity = new ClaimsIdentity(claims, isAuthenticated ? "Bearer" : null);
        return new ClaimsPrincipal(identity);
    }

    private static ClaimsPrincipal CreateAnonymousUser()
    {
        return new ClaimsPrincipal(new ClaimsIdentity());
    }

    private AuthorizationHandlerContext CreateContext(ClaimsPrincipal user, string permission)
    {
        var requirement = new PermissionRequirement(permission);
        return new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null);
    }

    #region HandleRequirementAsync Tests

    [Fact]
    public async Task HandleRequirementAsync_WithAnonymousUser_ShouldNotSucceed()
    {
        // Arrange
        var user = CreateAnonymousUser();
        var context = CreateContext(user, "test.permission");

        // Act
        await _sut.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirementAsync_WithNoNameIdentifierClaim_ShouldNotSucceed()
    {
        // Arrange
        var identity = new ClaimsIdentity(Array.Empty<Claim>(), "Bearer");
        var user = new ClaimsPrincipal(identity);
        var context = CreateContext(user, "test.permission");

        // Act
        await _sut.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirementAsync_WithNonExistentUser_ShouldNotSucceed()
    {
        // Arrange
        var user = CreateUser("nonexistent");
        var context = CreateContext(user, "test.permission");

        _userManagerMock.Setup(x => x.FindByIdAsync("nonexistent"))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        await _sut.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirementAsync_WithUserWithoutRoles_ShouldNotSucceed()
    {
        // Arrange
        var userId = "user123";
        var user = CreateUser(userId);
        var context = CreateContext(user, "test.permission");

        var appUser = new ApplicationUser { Id = userId, Email = "test@example.com", UserName = "test" };
        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(appUser);
        _userManagerMock.Setup(x => x.GetRolesAsync(appUser)).ReturnsAsync(new List<string>());

        // Act
        await _sut.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirementAsync_WithUserWithPermission_ShouldSucceed()
    {
        // Arrange
        var userId = "user123";
        var user = CreateUser(userId);
        var permission = "users.read";
        var context = CreateContext(user, permission);

        var appUser = new ApplicationUser { Id = userId, Email = "test@example.com", UserName = "test" };
        var role = new ApplicationRole { Id = "role1", Name = "Admin" };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(appUser);
        _userManagerMock.Setup(x => x.GetRolesAsync(appUser)).ReturnsAsync(new List<string> { "Admin" });
        _roleManagerMock.Setup(x => x.FindByNameAsync("Admin")).ReturnsAsync(role);
        _roleManagerMock.Setup(x => x.GetClaimsAsync(role))
            .ReturnsAsync(new List<Claim> { new(Permissions.ClaimType, permission) });

        // Act
        await _sut.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_WithUserWithoutRequiredPermission_ShouldNotSucceed()
    {
        // Arrange
        var userId = "user123";
        var user = CreateUser(userId);
        var context = CreateContext(user, "users.delete");

        var appUser = new ApplicationUser { Id = userId, Email = "test@example.com", UserName = "test" };
        var role = new ApplicationRole { Id = "role1", Name = "Viewer" };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(appUser);
        _userManagerMock.Setup(x => x.GetRolesAsync(appUser)).ReturnsAsync(new List<string> { "Viewer" });
        _roleManagerMock.Setup(x => x.FindByNameAsync("Viewer")).ReturnsAsync(role);
        _roleManagerMock.Setup(x => x.GetClaimsAsync(role))
            .ReturnsAsync(new List<Claim> { new(Permissions.ClaimType, "users.read") });

        // Act
        await _sut.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirementAsync_WithNonExistentRole_ShouldNotSucceed()
    {
        // Arrange
        var userId = "user123";
        var user = CreateUser(userId);
        var context = CreateContext(user, "test.permission");

        var appUser = new ApplicationUser { Id = userId, Email = "test@example.com", UserName = "test" };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(appUser);
        _userManagerMock.Setup(x => x.GetRolesAsync(appUser)).ReturnsAsync(new List<string> { "NonExistentRole" });
        _roleManagerMock.Setup(x => x.FindByNameAsync("NonExistentRole")).ReturnsAsync((ApplicationRole?)null);

        // Act
        await _sut.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirementAsync_WithMultipleRoles_ShouldCheckAllRoles()
    {
        // Arrange
        var userId = "user123";
        var user = CreateUser(userId);
        var permission = "users.delete";
        var context = CreateContext(user, permission);

        var appUser = new ApplicationUser { Id = userId, Email = "test@example.com", UserName = "test" };
        var viewerRole = new ApplicationRole { Id = "role1", Name = "Viewer" };
        var adminRole = new ApplicationRole { Id = "role2", Name = "Admin" };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(appUser);
        _userManagerMock.Setup(x => x.GetRolesAsync(appUser)).ReturnsAsync(new List<string> { "Viewer", "Admin" });
        _roleManagerMock.Setup(x => x.FindByNameAsync("Viewer")).ReturnsAsync(viewerRole);
        _roleManagerMock.Setup(x => x.FindByNameAsync("Admin")).ReturnsAsync(adminRole);
        _roleManagerMock.Setup(x => x.GetClaimsAsync(viewerRole))
            .ReturnsAsync(new List<Claim> { new(Permissions.ClaimType, "users.read") });
        _roleManagerMock.Setup(x => x.GetClaimsAsync(adminRole))
            .ReturnsAsync(new List<Claim> { new(Permissions.ClaimType, permission) });

        // Act
        await _sut.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue();
    }

    #endregion

    #region Caching Tests

    [Fact]
    public async Task HandleRequirementAsync_ShouldCachePermissions()
    {
        // Arrange
        var userId = "user123";
        var user = CreateUser(userId);
        var permission = "users.read";
        var context1 = CreateContext(user, permission);
        var context2 = CreateContext(user, permission);

        var appUser = new ApplicationUser { Id = userId, Email = "test@example.com", UserName = "test" };
        var role = new ApplicationRole { Id = "role1", Name = "Admin" };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(appUser);
        _userManagerMock.Setup(x => x.GetRolesAsync(appUser)).ReturnsAsync(new List<string> { "Admin" });
        _roleManagerMock.Setup(x => x.FindByNameAsync("Admin")).ReturnsAsync(role);
        _roleManagerMock.Setup(x => x.GetClaimsAsync(role))
            .ReturnsAsync(new List<Claim> { new(Permissions.ClaimType, permission) });

        // Act - First call
        await _sut.HandleAsync(context1);
        // Second call should use cache
        await _sut.HandleAsync(context2);

        // Assert
        context1.HasSucceeded.Should().BeTrue();
        context2.HasSucceeded.Should().BeTrue();
        // Verify FindByIdAsync was only called once (second call used cache)
        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldAcceptDependencies()
    {
        // Act
        var handler = new PermissionAuthorizationHandler(
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _cache);

        // Assert
        handler.Should().NotBeNull();
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public async Task HandleRequirementAsync_WithEmptyUserId_ShouldNotSucceed()
    {
        // Arrange
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "") };
        var identity = new ClaimsIdentity(claims, "Bearer");
        var user = new ClaimsPrincipal(identity);
        var context = CreateContext(user, "test.permission");

        // Act
        await _sut.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirementAsync_WithMultiplePermissions_ShouldCollectAll()
    {
        // Arrange
        var userId = "user123";
        var user = CreateUser(userId);
        var permission = "users.update";
        var context = CreateContext(user, permission);

        var appUser = new ApplicationUser { Id = userId, Email = "test@example.com", UserName = "test" };
        var role = new ApplicationRole { Id = "role1", Name = "Admin" };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(appUser);
        _userManagerMock.Setup(x => x.GetRolesAsync(appUser)).ReturnsAsync(new List<string> { "Admin" });
        _roleManagerMock.Setup(x => x.FindByNameAsync("Admin")).ReturnsAsync(role);
        _roleManagerMock.Setup(x => x.GetClaimsAsync(role))
            .ReturnsAsync(new List<Claim>
            {
                new(Permissions.ClaimType, "users.read"),
                new(Permissions.ClaimType, "users.create"),
                new(Permissions.ClaimType, "users.update"),
                new(Permissions.ClaimType, "users.delete")
            });

        // Act
        await _sut.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_WithNonPermissionClaims_ShouldIgnoreThem()
    {
        // Arrange
        var userId = "user123";
        var user = CreateUser(userId);
        var permission = "users.read";
        var context = CreateContext(user, permission);

        var appUser = new ApplicationUser { Id = userId, Email = "test@example.com", UserName = "test" };
        var role = new ApplicationRole { Id = "role1", Name = "Admin" };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(appUser);
        _userManagerMock.Setup(x => x.GetRolesAsync(appUser)).ReturnsAsync(new List<string> { "Admin" });
        _roleManagerMock.Setup(x => x.FindByNameAsync("Admin")).ReturnsAsync(role);
        _roleManagerMock.Setup(x => x.GetClaimsAsync(role))
            .ReturnsAsync(new List<Claim>
            {
                new("SomeOtherClaimType", "some-value"),
                new(Permissions.ClaimType, permission),
                new("AnotherType", "another-value")
            });

        // Act
        await _sut.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_CacheHit_ShouldNotCallUserManager()
    {
        // Arrange - Pre-populate cache
        var userId = "cached-user";
        var cacheKey = $"permissions:{userId}";
        var permissions = new HashSet<string> { "cached.permission" };
        _cache.Set(cacheKey, permissions);

        var user = CreateUser(userId);
        var context = CreateContext(user, "cached.permission");

        // Act
        await _sut.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue();
        _userManagerMock.Verify(x => x.FindByIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleRequirementAsync_CacheHit_WithMissingPermission_ShouldNotSucceed()
    {
        // Arrange - Pre-populate cache with different permissions
        var userId = "cached-user";
        var cacheKey = $"permissions:{userId}";
        var permissions = new HashSet<string> { "other.permission" };
        _cache.Set(cacheKey, permissions);

        var user = CreateUser(userId);
        var context = CreateContext(user, "required.permission");

        // Act
        await _sut.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirementAsync_WithRoleHavingNoClaims_ShouldNotSucceed()
    {
        // Arrange
        var userId = "user123";
        var user = CreateUser(userId);
        var context = CreateContext(user, "test.permission");

        var appUser = new ApplicationUser { Id = userId, Email = "test@example.com", UserName = "test" };
        var role = new ApplicationRole { Id = "role1", Name = "EmptyRole" };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(appUser);
        _userManagerMock.Setup(x => x.GetRolesAsync(appUser)).ReturnsAsync(new List<string> { "EmptyRole" });
        _roleManagerMock.Setup(x => x.FindByNameAsync("EmptyRole")).ReturnsAsync(role);
        _roleManagerMock.Setup(x => x.GetClaimsAsync(role)).ReturnsAsync(new List<Claim>());

        // Act
        await _sut.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse();
    }

    #endregion
}
