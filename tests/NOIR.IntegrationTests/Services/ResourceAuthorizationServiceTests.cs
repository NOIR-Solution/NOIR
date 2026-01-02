namespace NOIR.IntegrationTests.Services;

/// <summary>
/// Integration tests for ResourceAuthorizationService with SQL Server LocalDB.
/// Tests authorization logic including ownership, shares, and inheritance.
/// </summary>
[Collection("LocalDb")]
public class ResourceAuthorizationServiceTests : IAsyncLifetime
{
    private readonly LocalDbWebApplicationFactory _factory;

    public ResourceAuthorizationServiceTests(LocalDbWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    #region Test Helpers

    private class TestResource : IResource
    {
        public Guid Id { get; set; }
        public string ResourceType { get; set; } = "document";
        public string? OwnerId { get; set; }
        public Guid? ParentResourceId { get; set; }
        public string? ParentResourceType { get; set; }
    }

    #endregion

    #region Ownership Tests

    [Fact]
    public async Task AuthorizeAsync_WhenUserIsOwner_ShouldReturnTrue()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var authService = sp.GetRequiredService<IResourceAuthorizationService>();
            var userId = "owner-user";

            var resource = new TestResource
            {
                Id = Guid.NewGuid(),
                ResourceType = "document",
                OwnerId = userId
            };

            // Act
            var result = await authService.AuthorizeAsync(userId, resource, "read");

            // Assert
            result.Should().BeTrue();
        });
    }

    [Fact]
    public async Task AuthorizeAsync_WhenUserIsOwner_ShouldAllowAllActions()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var authService = sp.GetRequiredService<IResourceAuthorizationService>();
            var userId = "owner-user";

            var resource = new TestResource
            {
                Id = Guid.NewGuid(),
                ResourceType = "document",
                OwnerId = userId
            };

            // Act & Assert
            (await authService.AuthorizeAsync(userId, resource, "read")).Should().BeTrue();
            (await authService.AuthorizeAsync(userId, resource, "edit")).Should().BeTrue();
            (await authService.AuthorizeAsync(userId, resource, "delete")).Should().BeTrue();
            (await authService.AuthorizeAsync(userId, resource, "share")).Should().BeTrue();
        });
    }

    [Fact]
    public async Task GetEffectivePermissionAsync_WhenOwner_ShouldReturnAdmin()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var authService = sp.GetRequiredService<IResourceAuthorizationService>();
            var userId = "owner-user";

            var resource = new TestResource
            {
                Id = Guid.NewGuid(),
                ResourceType = "document",
                OwnerId = userId
            };

            // Act
            var result = await authService.GetEffectivePermissionAsync(userId, resource);

            // Assert
            result.Should().Be(SharePermission.Admin);
        });
    }

    #endregion

    #region Direct Share Tests

    [Fact]
    public async Task AuthorizeAsync_WhenUserHasDirectShare_ShouldReturnTrue()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();
            var authService = sp.GetRequiredService<IResourceAuthorizationService>();

            var resourceId = Guid.NewGuid();
            var userId = "shared-user";

            // Create a share
            var share = ResourceShare.Create(
                "document",
                resourceId,
                userId,
                SharePermission.Edit);

            context.ResourceShares.Add(share);
            await context.SaveChangesAsync();

            var resource = new TestResource
            {
                Id = resourceId,
                ResourceType = "document",
                OwnerId = "other-owner"
            };

            // Act
            var result = await authService.AuthorizeAsync(userId, resource, "edit");

            // Assert
            result.Should().BeTrue();
        });
    }

    [Fact]
    public async Task AuthorizeAsync_WhenUserHasInsufficientPermission_ShouldReturnFalse()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();
            var authService = sp.GetRequiredService<IResourceAuthorizationService>();

            var resourceId = Guid.NewGuid();
            var userId = "limited-user";

            // Create a View-only share
            var share = ResourceShare.Create(
                "document",
                resourceId,
                userId,
                SharePermission.View);

            context.ResourceShares.Add(share);
            await context.SaveChangesAsync();

            var resource = new TestResource
            {
                Id = resourceId,
                ResourceType = "document",
                OwnerId = "other-owner"
            };

            // Act - Try to edit with View permission
            var result = await authService.AuthorizeAsync(userId, resource, "edit");

            // Assert
            result.Should().BeFalse();
        });
    }

    [Fact]
    public async Task AuthorizeAsync_WhenShareExpired_ShouldReturnFalse()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();
            var authService = sp.GetRequiredService<IResourceAuthorizationService>();

            var resourceId = Guid.NewGuid();
            var userId = "expired-share-user";

            // Create an expired share
            var share = ResourceShare.Create(
                "document",
                resourceId,
                userId,
                SharePermission.Admin,
                expiresAt: DateTimeOffset.UtcNow.AddHours(-1));

            context.ResourceShares.Add(share);
            await context.SaveChangesAsync();

            var resource = new TestResource
            {
                Id = resourceId,
                ResourceType = "document",
                OwnerId = "other-owner"
            };

            // Act
            var result = await authService.AuthorizeAsync(userId, resource, "read");

            // Assert
            result.Should().BeFalse();
        });
    }

    [Fact]
    public async Task GetEffectivePermissionAsync_WhenHasShare_ShouldReturnSharePermission()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();
            var authService = sp.GetRequiredService<IResourceAuthorizationService>();

            var resourceId = Guid.NewGuid();
            var userId = "shared-user";

            var share = ResourceShare.Create(
                "document",
                resourceId,
                userId,
                SharePermission.Comment);

            context.ResourceShares.Add(share);
            await context.SaveChangesAsync();

            var resource = new TestResource
            {
                Id = resourceId,
                ResourceType = "document",
                OwnerId = "other-owner"
            };

            // Act
            var result = await authService.GetEffectivePermissionAsync(userId, resource);

            // Assert
            result.Should().Be(SharePermission.Comment);
        });
    }

    #endregion

    #region Permission Inheritance Tests

    [Fact]
    public async Task AuthorizeAsync_WhenParentHasShare_ShouldInheritPermission()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();
            var authService = sp.GetRequiredService<IResourceAuthorizationService>();

            var parentId = Guid.NewGuid();
            var childId = Guid.NewGuid();
            var userId = "inherited-user";

            // Create share on parent folder
            var share = ResourceShare.Create(
                "folder",
                parentId,
                userId,
                SharePermission.Edit);

            context.ResourceShares.Add(share);
            await context.SaveChangesAsync();

            // Child resource with parent
            var childResource = new TestResource
            {
                Id = childId,
                ResourceType = "document",
                OwnerId = "other-owner",
                ParentResourceId = parentId,
                ParentResourceType = "folder"
            };

            // Act
            var result = await authService.AuthorizeAsync(userId, childResource, "edit");

            // Assert
            result.Should().BeTrue();
        });
    }

    [Fact]
    public async Task GetEffectivePermissionAsync_WhenInherited_ShouldReturnParentPermission()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();
            var authService = sp.GetRequiredService<IResourceAuthorizationService>();

            var parentId = Guid.NewGuid();
            var childId = Guid.NewGuid();
            var userId = "inherited-user";

            // Create share on parent
            var share = ResourceShare.Create(
                "folder",
                parentId,
                userId,
                SharePermission.Admin);

            context.ResourceShares.Add(share);
            await context.SaveChangesAsync();

            var childResource = new TestResource
            {
                Id = childId,
                ResourceType = "document",
                OwnerId = "other-owner",
                ParentResourceId = parentId,
                ParentResourceType = "folder"
            };

            // Act
            var result = await authService.GetEffectivePermissionAsync(userId, childResource);

            // Assert
            result.Should().Be(SharePermission.Admin);
        });
    }

    #endregion

    #region No Access Tests

    [Fact]
    public async Task AuthorizeAsync_WhenNoAccessExists_ShouldReturnFalse()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var authService = sp.GetRequiredService<IResourceAuthorizationService>();
            var userId = "no-access-user";

            var resource = new TestResource
            {
                Id = Guid.NewGuid(),
                ResourceType = "document",
                OwnerId = "other-owner"
            };

            // Act
            var result = await authService.AuthorizeAsync(userId, resource, "read");

            // Assert
            result.Should().BeFalse();
        });
    }

    [Fact]
    public async Task GetEffectivePermissionAsync_WhenNoAccess_ShouldReturnNull()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var authService = sp.GetRequiredService<IResourceAuthorizationService>();
            var userId = "no-access-user";

            var resource = new TestResource
            {
                Id = Guid.NewGuid(),
                ResourceType = "document",
                OwnerId = "other-owner"
            };

            // Act
            var result = await authService.GetEffectivePermissionAsync(userId, resource);

            // Assert
            result.Should().BeNull();
        });
    }

    #endregion

    #region GetAccessibleResourcesAsync Tests

    [Fact]
    public async Task GetAccessibleResourcesAsync_ShouldReturnAllSharedResources()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();
            var authService = sp.GetRequiredService<IResourceAuthorizationService>();
            var userId = "multi-share-user";

            // Create multiple shares
            var share1 = ResourceShare.Create("document", Guid.NewGuid(), userId, SharePermission.View);
            var share2 = ResourceShare.Create("document", Guid.NewGuid(), userId, SharePermission.Edit);
            var share3 = ResourceShare.Create("document", Guid.NewGuid(), userId, SharePermission.Admin);

            context.ResourceShares.AddRange(share1, share2, share3);
            await context.SaveChangesAsync();

            // Act
            var results = await authService.GetAccessibleResourcesAsync(userId, "document");

            // Assert
            results.Should().HaveCount(3);
            results.Select(r => r.Permission).Should().BeEquivalentTo([
                SharePermission.View,
                SharePermission.Edit,
                SharePermission.Admin
            ]);
        });
    }

    #endregion
}
