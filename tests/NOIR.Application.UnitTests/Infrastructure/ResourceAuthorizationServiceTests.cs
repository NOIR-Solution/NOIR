namespace NOIR.Application.UnitTests.Infrastructure;

/// <summary>
/// Unit tests for ResourceAuthorizationService.
/// Tests resource-based authorization with ownership, validation, and caching.
/// Note: Database interaction tests are in integration tests due to EF Core async query complexity.
/// </summary>
public class ResourceAuthorizationServiceTests
{
    private readonly Mock<ApplicationDbContext> _dbContextMock;
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<ResourceAuthorizationService>> _loggerMock;
    private readonly ResourceAuthorizationService _sut;

    public ResourceAuthorizationServiceTests()
    {
        _dbContextMock = new Mock<ApplicationDbContext>(new DbContextOptions<ApplicationDbContext>());
        _cache = new MemoryCache(new MemoryCacheOptions());
        _loggerMock = new Mock<ILogger<ResourceAuthorizationService>>();

        _sut = new ResourceAuthorizationService(
            _dbContextMock.Object,
            _cache,
            _loggerMock.Object);
    }

    #region AuthorizeAsync - Ownership Tests

    [Fact]
    public async Task AuthorizeAsync_WhenUserIsOwner_ReturnsTrue()
    {
        // Arrange
        var userId = "user-123";
        var resource = CreateMockResource(ownerId: userId);

        // Act
        var result = await _sut.AuthorizeAsync(userId, resource, "read");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task AuthorizeAsync_WhenOwner_AllowsReadAction()
    {
        // Arrange
        var userId = "user-123";
        var resource = CreateMockResource(ownerId: userId);

        // Act
        var result = await _sut.AuthorizeAsync(userId, resource, "read");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task AuthorizeAsync_WhenOwner_AllowsEditAction()
    {
        // Arrange
        var userId = "user-123";
        var resource = CreateMockResource(ownerId: userId);

        // Act
        var result = await _sut.AuthorizeAsync(userId, resource, "edit");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task AuthorizeAsync_WhenOwner_AllowsDeleteAction()
    {
        // Arrange
        var userId = "user-123";
        var resource = CreateMockResource(ownerId: userId);

        // Act
        var result = await _sut.AuthorizeAsync(userId, resource, "delete");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task AuthorizeAsync_WhenOwner_AllowsAdminAction()
    {
        // Arrange
        var userId = "user-123";
        var resource = CreateMockResource(ownerId: userId);

        // Act
        var result = await _sut.AuthorizeAsync(userId, resource, "admin");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task AuthorizeAsync_WhenOwner_AllowsShareAction()
    {
        // Arrange
        var userId = "user-123";
        var resource = CreateMockResource(ownerId: userId);

        // Act
        var result = await _sut.AuthorizeAsync(userId, resource, "share");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task AuthorizeAsync_WhenOwner_AllowsCommentAction()
    {
        // Arrange
        var userId = "user-123";
        var resource = CreateMockResource(ownerId: userId);

        // Act
        var result = await _sut.AuthorizeAsync(userId, resource, "comment");

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region GetEffectivePermissionAsync - Ownership Tests

    [Fact]
    public async Task GetEffectivePermissionAsync_WhenOwner_ReturnsAdmin()
    {
        // Arrange
        var userId = "user-123";
        var resource = CreateMockResource(ownerId: userId);

        // Act
        var result = await _sut.GetEffectivePermissionAsync(userId, resource);

        // Assert
        result.Should().Be(SharePermission.Admin);
    }

    #endregion

    #region AuthorizeAsync - Validation Tests

    [Fact]
    public async Task AuthorizeAsync_WithNullResource_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _sut.AuthorizeAsync("user", default(IResource)!, "read");

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AuthorizeAsync_WithInvalidUserId_ThrowsArgumentException(string? userId)
    {
        // Arrange
        var resource = CreateMockResource();

        // Act
        var act = () => _sut.AuthorizeAsync(userId!, resource, "read");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AuthorizeAsync_WithInvalidAction_ThrowsArgumentException(string? action)
    {
        // Arrange
        var resource = CreateMockResource();

        // Act
        var act = () => _sut.AuthorizeAsync("user", resource, action!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region AuthorizeAsync by ResourceType/ResourceId - Validation Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AuthorizeAsync_ByResourceTypeAndId_WithInvalidUserId_ThrowsArgumentException(string? userId)
    {
        // Act
        var act = () => _sut.AuthorizeAsync(userId!, "document", Guid.NewGuid(), "read");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AuthorizeAsync_ByResourceTypeAndId_WithInvalidResourceType_ThrowsArgumentException(string? resourceType)
    {
        // Act
        var act = () => _sut.AuthorizeAsync("user", resourceType!, Guid.NewGuid(), "read");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AuthorizeAsync_ByResourceTypeAndId_WithInvalidAction_ThrowsArgumentException(string? action)
    {
        // Act
        var act = () => _sut.AuthorizeAsync("user", "document", Guid.NewGuid(), action!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task AuthorizeAsync_ByResourceTypeAndId_WithUnknownAction_ReturnsFalse()
    {
        // Arrange - unknown action should return false immediately without DB query
        // because FromAction returns null

        // Act
        var result = await _sut.AuthorizeAsync("user", "document", Guid.NewGuid(), "unknown-action-xyz");

        // Assert - Unknown action returns false immediately (FromAction returns null)
        result.Should().BeFalse();
    }

    #endregion

    #region GetEffectivePermissionAsync - Validation Tests

    [Fact]
    public async Task GetEffectivePermissionAsync_WithNullResource_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _sut.GetEffectivePermissionAsync("user", null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetEffectivePermissionAsync_WithInvalidUserId_ThrowsArgumentException(string? userId)
    {
        // Arrange
        var resource = CreateMockResource();

        // Act
        var act = () => _sut.GetEffectivePermissionAsync(userId!, resource);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region GetAccessibleResourcesAsync - Validation Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetAccessibleResourcesAsync_WithInvalidUserId_ThrowsArgumentException(string? userId)
    {
        // Act
        var act = () => _sut.GetAccessibleResourcesAsync(userId!, "document");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetAccessibleResourcesAsync_WithInvalidResourceType_ThrowsArgumentException(string? resourceType)
    {
        // Act
        var act = () => _sut.GetAccessibleResourcesAsync("user", resourceType!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region Caching Tests

    [Fact]
    public async Task AuthorizeAsync_ByResourceTypeAndId_UsesCachedPermission()
    {
        // Arrange
        var userId = "user-123";
        var resourceId = Guid.NewGuid();
        var resourceType = "document";

        // Pre-populate cache with Edit permission
        var cacheKey = $"resource_auth:{resourceType}:{resourceId}:{userId}";
        _cache.Set(cacheKey, (SharePermission?)SharePermission.Edit);

        // Act - Should use cached value without DB query
        var result = await _sut.AuthorizeAsync(userId, resourceType, resourceId, "edit");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task AuthorizeAsync_ByResourceTypeAndId_CacheHitWithInsufficientPermission_ReturnsFalse()
    {
        // Arrange
        var userId = "user-123";
        var resourceId = Guid.NewGuid();
        var resourceType = "document";

        // Pre-populate cache with View permission
        var cacheKey = $"resource_auth:{resourceType}:{resourceId}:{userId}";
        _cache.Set(cacheKey, (SharePermission?)SharePermission.View);

        // Act - Try to edit with only View permission
        var result = await _sut.AuthorizeAsync(userId, resourceType, resourceId, "edit");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AuthorizeAsync_ByResourceTypeAndId_CacheHitWithNullPermission_ReturnsFalse()
    {
        // Arrange
        var userId = "user-123";
        var resourceId = Guid.NewGuid();
        var resourceType = "document";

        // Pre-populate cache with null (no permission)
        var cacheKey = $"resource_auth:{resourceType}:{resourceId}:{userId}";
        _cache.Set(cacheKey, (SharePermission?)null);

        // Act
        var result = await _sut.AuthorizeAsync(userId, resourceType, resourceId, "read");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetEffectivePermissionAsync_UsesCachedPermission()
    {
        // Arrange
        var userId = "user-123";
        var resourceId = Guid.NewGuid();
        var resource = CreateMockResource(id: resourceId, ownerId: "other-user", resourceType: "document");

        // Pre-populate cache with Comment permission
        var cacheKey = $"resource_perm:document:{resourceId}:{userId}";
        _cache.Set(cacheKey, (SharePermission?)SharePermission.Comment);

        // Act - Should use cached value without DB query
        var result = await _sut.GetEffectivePermissionAsync(userId, resource);

        // Assert
        result.Should().Be(SharePermission.Comment);
    }

    [Fact]
    public async Task GetEffectivePermissionAsync_CacheHitWithNull_ReturnsNull()
    {
        // Arrange
        var userId = "user-123";
        var resourceId = Guid.NewGuid();
        var resource = CreateMockResource(id: resourceId, ownerId: "other-user", resourceType: "document");

        // Pre-populate cache with null
        var cacheKey = $"resource_perm:document:{resourceId}:{userId}";
        _cache.Set(cacheKey, (SharePermission?)null);

        // Act
        var result = await _sut.GetEffectivePermissionAsync(userId, resource);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region SharePermission and Action Mapping Tests

    [Theory]
    [InlineData("read", SharePermission.View, true)]
    [InlineData("view", SharePermission.View, true)]
    [InlineData("comment", SharePermission.Comment, true)]
    [InlineData("comment", SharePermission.View, false)]
    [InlineData("edit", SharePermission.Edit, true)]
    [InlineData("edit", SharePermission.Comment, false)]
    [InlineData("update", SharePermission.Edit, true)]
    [InlineData("write", SharePermission.Edit, true)]
    [InlineData("delete", SharePermission.Admin, true)]
    [InlineData("delete", SharePermission.Edit, false)]
    [InlineData("admin", SharePermission.Admin, true)]
    [InlineData("share", SharePermission.Admin, true)]
    [InlineData("manage", SharePermission.Admin, true)]
    public async Task AuthorizeAsync_ActionPermissionMapping_WorksCorrectly(
        string action,
        SharePermission cachedPermission,
        bool expectedResult)
    {
        // Arrange
        var userId = "user-123";
        var resourceId = Guid.NewGuid();
        var resourceType = "document";

        // Use cache to control the permission
        var cacheKey = $"resource_auth:{resourceType}:{resourceId}:{userId}";
        _cache.Set(cacheKey, (SharePermission?)cachedPermission);

        // Act
        var result = await _sut.AuthorizeAsync(userId, resourceType, resourceId, action);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task AuthorizeAsync_HigherPermissionIncludesLowerActions()
    {
        // Arrange
        var userId = "user-123";
        var resourceId = Guid.NewGuid();
        var resourceType = "document";

        // Cache Admin permission
        var cacheKey = $"resource_auth:{resourceType}:{resourceId}:{userId}";
        _cache.Set(cacheKey, (SharePermission?)SharePermission.Admin);

        // Act & Assert - Admin should allow all actions
        (await _sut.AuthorizeAsync(userId, resourceType, resourceId, "read")).Should().BeTrue();
        (await _sut.AuthorizeAsync(userId, resourceType, resourceId, "comment")).Should().BeTrue();
        (await _sut.AuthorizeAsync(userId, resourceType, resourceId, "edit")).Should().BeTrue();
        (await _sut.AuthorizeAsync(userId, resourceType, resourceId, "delete")).Should().BeTrue();
        (await _sut.AuthorizeAsync(userId, resourceType, resourceId, "admin")).Should().BeTrue();
    }

    [Fact]
    public async Task AuthorizeAsync_EditPermissionIncludesViewAndComment()
    {
        // Arrange
        var userId = "user-123";
        var resourceId = Guid.NewGuid();
        var resourceType = "document";

        // Cache Edit permission
        var cacheKey = $"resource_auth:{resourceType}:{resourceId}:{userId}";
        _cache.Set(cacheKey, (SharePermission?)SharePermission.Edit);

        // Act & Assert
        (await _sut.AuthorizeAsync(userId, resourceType, resourceId, "read")).Should().BeTrue();
        (await _sut.AuthorizeAsync(userId, resourceType, resourceId, "comment")).Should().BeTrue();
        (await _sut.AuthorizeAsync(userId, resourceType, resourceId, "edit")).Should().BeTrue();
        (await _sut.AuthorizeAsync(userId, resourceType, resourceId, "delete")).Should().BeFalse();
        (await _sut.AuthorizeAsync(userId, resourceType, resourceId, "admin")).Should().BeFalse();
    }

    #endregion

    #region Logging Tests

    [Fact]
    public async Task AuthorizeAsync_WhenOwner_LogsDebugMessage()
    {
        // Arrange
        var userId = "user-123";
        var resource = CreateMockResource(ownerId: userId, resourceType: "document");

        // Act
        await _sut.AuthorizeAsync(userId, resource, "read");

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("owner")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldAcceptDependencies()
    {
        // Act
        var service = new ResourceAuthorizationService(
            _dbContextMock.Object,
            _cache,
            _loggerMock.Object);

        // Assert
        service.Should().NotBeNull();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task AuthorizeAsync_WhenOwnerIdIsNull_AndUserIsNotEmpty_ReturnsFalseWithoutOwnerMatch()
    {
        // Arrange
        var userId = "user-123";
        var resourceId = Guid.NewGuid();
        var resource = CreateMockResource(id: resourceId, ownerId: null, resourceType: "document");

        // Cache no permission
        var cacheKey = $"resource_perm:document:{resourceId}:{userId}";
        _cache.Set(cacheKey, (SharePermission?)null);

        // Act
        var result = await _sut.AuthorizeAsync(userId, resource, "read");

        // Assert - null != "user-123" so not owner, and cache has null permission
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AuthorizeAsync_ResourceTypeCaseSensitivity_HandledCorrectly()
    {
        // Arrange - resource type should be normalized (service uses ToLowerInvariant)
        var userId = "user-123";
        var resourceId = Guid.NewGuid();

        // Cache with lowercase key (how the service stores it)
        var cacheKey = $"resource_auth:document:{resourceId}:{userId}";
        _cache.Set(cacheKey, (SharePermission?)SharePermission.View);

        // Act - Pass lowercase (matches cache key)
        var result = await _sut.AuthorizeAsync(userId, "document", resourceId, "read");

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("Document")]
    [InlineData("DOCUMENT")]
    [InlineData("DoCuMeNt")]
    public async Task AuthorizeAsync_ResourceTypeMixedCase_NormalizesToLowercase(string resourceType)
    {
        // Arrange - cache key should be normalized to lowercase
        var userId = "user-123";
        var resourceId = Guid.NewGuid();

        // Pre-populate cache with lowercase key (how the service normalizes it)
        var cacheKey = $"resource_auth:document:{resourceId}:{userId}";
        _cache.Set(cacheKey, (SharePermission?)SharePermission.Edit);

        // Act - Pass mixed case resourceType
        var result = await _sut.AuthorizeAsync(userId, resourceType, resourceId, "edit");

        // Assert - Should find cached permission regardless of case
        result.Should().BeTrue();
    }

    #endregion

    #region Helper Methods

    private static IResource CreateMockResource(
        Guid? id = null,
        string resourceType = "document",
        string? ownerId = null,
        Guid? parentResourceId = null,
        string? parentResourceType = null)
    {
        var mock = new Mock<IResource>();
        mock.Setup(r => r.Id).Returns(id ?? Guid.NewGuid());
        mock.Setup(r => r.ResourceType).Returns(resourceType);
        mock.Setup(r => r.OwnerId).Returns(ownerId);
        mock.Setup(r => r.ParentResourceId).Returns(parentResourceId);
        mock.Setup(r => r.ParentResourceType).Returns(parentResourceType);
        return mock.Object;
    }

    #endregion
}
