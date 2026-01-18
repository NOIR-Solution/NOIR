namespace NOIR.Application.UnitTests.Infrastructure;

/// <summary>
/// Unit tests for CacheInvalidationService.
/// Tests cache invalidation operations.
/// </summary>
public class CacheInvalidationServiceTests
{
    private readonly Mock<IFusionCache> _mockCache;
    private readonly Mock<ILogger<CacheInvalidationService>> _mockLogger;
    private readonly CacheInvalidationService _sut;

    public CacheInvalidationServiceTests()
    {
        _mockCache = new Mock<IFusionCache>();
        _mockLogger = new Mock<ILogger<CacheInvalidationService>>();
        _sut = new CacheInvalidationService(_mockCache.Object, _mockLogger.Object);
    }

    #region InvalidateUserCacheAsync Tests

    [Fact]
    public async Task InvalidateUserCacheAsync_ShouldRemoveAllUserRelatedKeys()
    {
        // Arrange
        var userId = "user-123";

        // Act
        await _sut.InvalidateUserCacheAsync(userId);

        // Assert
        _mockCache.Verify(c => c.RemoveAsync(
            CacheKeys.UserProfile(userId),
            It.IsAny<FusionCacheEntryOptions?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);

        _mockCache.Verify(c => c.RemoveAsync(
            CacheKeys.UserById(userId),
            It.IsAny<FusionCacheEntryOptions?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);

        _mockCache.Verify(c => c.RemoveAsync(
            CacheKeys.UserPermissions(userId),
            It.IsAny<FusionCacheEntryOptions?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task InvalidateUserCacheAsync_ShouldLogInformation()
    {
        // Arrange
        var userId = "user-123";

        // Act
        await _sut.InvalidateUserCacheAsync(userId);

        // Assert
        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(userId)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region InvalidateUserPermissionsAsync Tests

    [Fact]
    public async Task InvalidateUserPermissionsAsync_ShouldRemovePermissionKey()
    {
        // Arrange
        var userId = "user-123";

        // Act
        await _sut.InvalidateUserPermissionsAsync(userId);

        // Assert
        _mockCache.Verify(c => c.RemoveAsync(
            CacheKeys.UserPermissions(userId),
            It.IsAny<FusionCacheEntryOptions?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region InvalidateRoleCacheAsync Tests

    [Fact]
    public async Task InvalidateRoleCacheAsync_ShouldRemoveAllRoleRelatedKeys()
    {
        // Arrange
        var roleId = "role-456";

        // Act
        await _sut.InvalidateRoleCacheAsync(roleId);

        // Assert
        _mockCache.Verify(c => c.RemoveAsync(
            CacheKeys.RoleById(roleId),
            It.IsAny<FusionCacheEntryOptions?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);

        _mockCache.Verify(c => c.RemoveAsync(
            CacheKeys.RolePermissions(roleId),
            It.IsAny<FusionCacheEntryOptions?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);

        _mockCache.Verify(c => c.RemoveAsync(
            CacheKeys.AllRoles(),
            It.IsAny<FusionCacheEntryOptions?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region InvalidateAllPermissionsAsync Tests

    [Fact]
    public async Task InvalidateAllPermissionsAsync_ShouldLogWarning()
    {
        // Act
        await _sut.InvalidateAllPermissionsAsync();

        // Assert
        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Full permission cache invalidation")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region InvalidateBlogCacheAsync Tests

    [Fact]
    public async Task InvalidateBlogCacheAsync_ShouldRemoveAllBlogRelatedKeys()
    {
        // Act
        await _sut.InvalidateBlogCacheAsync();

        // Assert
        _mockCache.Verify(c => c.RemoveAsync(
            CacheKeys.BlogCategories(),
            It.IsAny<FusionCacheEntryOptions?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);

        _mockCache.Verify(c => c.RemoveAsync(
            CacheKeys.BlogTags(),
            It.IsAny<FusionCacheEntryOptions?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);

        _mockCache.Verify(c => c.RemoveAsync(
            CacheKeys.RssFeed(),
            It.IsAny<FusionCacheEntryOptions?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);

        _mockCache.Verify(c => c.RemoveAsync(
            CacheKeys.Sitemap(),
            It.IsAny<FusionCacheEntryOptions?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region InvalidatePostCacheAsync Tests

    [Fact]
    public async Task InvalidatePostCacheAsync_ShouldRemovePostByIdKey()
    {
        // Arrange
        var postId = Guid.NewGuid();

        // Act
        await _sut.InvalidatePostCacheAsync(postId);

        // Assert
        _mockCache.Verify(c => c.RemoveAsync(
            CacheKeys.PostById(postId),
            It.IsAny<FusionCacheEntryOptions?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task InvalidatePostCacheAsync_WithSlug_ShouldRemovePostBySlugKey()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var slug = "my-post-slug";

        // Act
        await _sut.InvalidatePostCacheAsync(postId, slug);

        // Assert
        _mockCache.Verify(c => c.RemoveAsync(
            CacheKeys.PostBySlug(slug),
            It.IsAny<FusionCacheEntryOptions?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task InvalidatePostCacheAsync_WithoutSlug_ShouldNotRemovePostBySlugKey()
    {
        // Arrange
        var postId = Guid.NewGuid();

        // Act
        await _sut.InvalidatePostCacheAsync(postId);

        // Assert - should not call RemoveAsync for any slug key
        _mockCache.Verify(c => c.RemoveAsync(
            It.Is<string>(k => k.StartsWith("blog:post:slug:")),
            It.IsAny<FusionCacheEntryOptions?>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task InvalidatePostCacheAsync_ShouldInvalidateFeeds()
    {
        // Arrange
        var postId = Guid.NewGuid();

        // Act
        await _sut.InvalidatePostCacheAsync(postId);

        // Assert
        _mockCache.Verify(c => c.RemoveAsync(
            CacheKeys.RssFeed(),
            It.IsAny<FusionCacheEntryOptions?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);

        _mockCache.Verify(c => c.RemoveAsync(
            CacheKeys.Sitemap(),
            It.IsAny<FusionCacheEntryOptions?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region InvalidateTenantSettingsAsync Tests

    [Fact]
    public async Task InvalidateTenantSettingsAsync_ShouldRemoveAllTenantRelatedKeys()
    {
        // Arrange
        var tenantId = "tenant-789";

        // Act
        await _sut.InvalidateTenantSettingsAsync(tenantId);

        // Assert
        _mockCache.Verify(c => c.RemoveAsync(
            CacheKeys.TenantSettings(tenantId),
            It.IsAny<FusionCacheEntryOptions?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);

        _mockCache.Verify(c => c.RemoveAsync(
            CacheKeys.TenantById(tenantId),
            It.IsAny<FusionCacheEntryOptions?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Service Implementation Tests

    [Fact]
    public void CacheInvalidationService_ShouldImplementIScopedService()
    {
        // Assert
        _sut.Should().BeAssignableTo<IScopedService>();
    }

    [Fact]
    public void CacheInvalidationService_ShouldImplementICacheInvalidationService()
    {
        // Assert
        _sut.Should().BeAssignableTo<ICacheInvalidationService>();
    }

    #endregion
}
