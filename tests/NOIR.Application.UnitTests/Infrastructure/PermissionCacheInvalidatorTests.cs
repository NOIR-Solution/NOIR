namespace NOIR.Application.UnitTests.Infrastructure;

/// <summary>
/// Unit tests for PermissionCacheInvalidator.
/// Tests cache invalidation scenarios and thread-safety.
/// </summary>
public class PermissionCacheInvalidatorTests
{
    private readonly Mock<IMemoryCache> _cacheMock;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly PermissionCacheInvalidator _sut;

    public PermissionCacheInvalidatorTests()
    {
        _cacheMock = new Mock<IMemoryCache>();

        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _sut = new PermissionCacheInvalidator(_cacheMock.Object, _userManagerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldAcceptDependencies()
    {
        // Act
        var invalidator = new PermissionCacheInvalidator(_cacheMock.Object, _userManagerMock.Object);

        // Assert
        invalidator.Should().NotBeNull();
    }

    [Fact]
    public void Service_ShouldImplementIPermissionCacheInvalidator()
    {
        // Assert
        _sut.Should().BeAssignableTo<IPermissionCacheInvalidator>();
    }

    [Fact]
    public void Service_ShouldImplementIScopedService()
    {
        // Assert
        _sut.Should().BeAssignableTo<IScopedService>();
    }

    #endregion

    #region RegisterCachedUser Tests

    [Fact]
    public void RegisterCachedUser_ShouldNotThrow()
    {
        // Act
        var act = () => PermissionCacheInvalidator.RegisterCachedUser("test-user");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RegisterCachedUser_WithSameUserTwice_ShouldNotThrow()
    {
        // Act
        var act = () =>
        {
            PermissionCacheInvalidator.RegisterCachedUser("duplicate-user");
            PermissionCacheInvalidator.RegisterCachedUser("duplicate-user");
        };

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region InvalidateUser Tests

    [Fact]
    public void InvalidateUser_ShouldCallCacheRemove()
    {
        // Arrange
        var userId = "user-to-invalidate";

        // Act
        _sut.InvalidateUser(userId);

        // Assert
        _cacheMock.Verify(x => x.Remove($"permissions:{userId}"), Times.Once);
    }

    [Fact]
    public void InvalidateUser_ShouldUseCorrectCacheKeyFormat()
    {
        // Arrange
        var userId = "test-user-123";
        object? removedKey = null;
        _cacheMock.Setup(x => x.Remove(It.IsAny<object>()))
            .Callback<object>(key => removedKey = key);

        // Act
        _sut.InvalidateUser(userId);

        // Assert
        removedKey.Should().Be("permissions:test-user-123");
    }

    [Fact]
    public void InvalidateUser_WithEmptyUserId_ShouldStillCallCacheRemove()
    {
        // Act
        _sut.InvalidateUser(string.Empty);

        // Assert
        _cacheMock.Verify(x => x.Remove("permissions:"), Times.Once);
    }

    #endregion

    #region InvalidateRoleAsync Tests

    [Fact]
    public async Task InvalidateRoleAsync_ShouldQueryUsersInRole()
    {
        // Arrange
        var roleName = "Admin";
        _userManagerMock.Setup(x => x.GetUsersInRoleAsync(roleName))
            .ReturnsAsync(new List<ApplicationUser>());

        // Act
        await _sut.InvalidateRoleAsync(roleName);

        // Assert
        _userManagerMock.Verify(x => x.GetUsersInRoleAsync(roleName), Times.Once);
    }

    [Fact]
    public async Task InvalidateRoleAsync_WithUsersInRole_ShouldInvalidateEachUser()
    {
        // Arrange
        var roleName = "Admin";
        var users = new List<ApplicationUser>
        {
            new() { Id = "user-1" },
            new() { Id = "user-2" },
            new() { Id = "user-3" }
        };
        _userManagerMock.Setup(x => x.GetUsersInRoleAsync(roleName))
            .ReturnsAsync(users);

        // Act
        await _sut.InvalidateRoleAsync(roleName);

        // Assert
        _cacheMock.Verify(x => x.Remove("permissions:user-1"), Times.Once);
        _cacheMock.Verify(x => x.Remove("permissions:user-2"), Times.Once);
        _cacheMock.Verify(x => x.Remove("permissions:user-3"), Times.Once);
    }

    [Fact]
    public async Task InvalidateRoleAsync_WithNoUsersInRole_ShouldNotCallCacheRemove()
    {
        // Arrange
        _userManagerMock.Setup(x => x.GetUsersInRoleAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<ApplicationUser>());

        // Act
        await _sut.InvalidateRoleAsync("EmptyRole");

        // Assert
        _cacheMock.Verify(x => x.Remove(It.IsAny<object>()), Times.Never);
    }

    #endregion

    #region InvalidateAll Tests

    [Fact]
    public void InvalidateAll_ShouldNotThrow()
    {
        // Act
        var act = () => _sut.InvalidateAll();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void InvalidateAll_AfterRegisteringUsers_ShouldInvalidateAllRegistered()
    {
        // Arrange - Register some users first
        PermissionCacheInvalidator.RegisterCachedUser("cached-user-1");
        PermissionCacheInvalidator.RegisterCachedUser("cached-user-2");

        // Act
        _sut.InvalidateAll();

        // Assert - Cache.Remove should have been called (exact count depends on static state)
        _cacheMock.Verify(x => x.Remove(It.IsAny<object>()), Times.AtLeastOnce);
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public void RegisterCachedUser_ConcurrentCalls_ShouldNotThrow()
    {
        // Act
        var act = () => Parallel.For(0, 100, i =>
        {
            PermissionCacheInvalidator.RegisterCachedUser($"concurrent-user-{i}");
        });

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void InvalidateAll_ConcurrentWithRegister_ShouldNotThrow()
    {
        // Act
        var act = () =>
        {
            var registerTask = Task.Run(() =>
            {
                for (int i = 0; i < 50; i++)
                {
                    PermissionCacheInvalidator.RegisterCachedUser($"thread-user-{i}");
                }
            });

            var invalidateTask = Task.Run(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    _sut.InvalidateAll();
                }
            });

            Task.WaitAll(registerTask, invalidateTask);
        };

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Method Existence Tests

    [Fact]
    public void InvalidateUser_MethodShouldExist()
    {
        // Assert
        var method = typeof(PermissionCacheInvalidator).GetMethod("InvalidateUser");
        method.Should().NotBeNull();
    }

    [Fact]
    public void InvalidateRoleAsync_MethodShouldExist()
    {
        // Assert
        var method = typeof(PermissionCacheInvalidator).GetMethod("InvalidateRoleAsync");
        method.Should().NotBeNull();
    }

    [Fact]
    public void InvalidateAll_MethodShouldExist()
    {
        // Assert
        var method = typeof(PermissionCacheInvalidator).GetMethod("InvalidateAll");
        method.Should().NotBeNull();
    }

    [Fact]
    public void RegisterCachedUser_StaticMethodShouldExist()
    {
        // Assert
        var method = typeof(PermissionCacheInvalidator).GetMethod("RegisterCachedUser");
        method.Should().NotBeNull();
        method!.IsStatic.Should().BeTrue();
    }

    #endregion
}
