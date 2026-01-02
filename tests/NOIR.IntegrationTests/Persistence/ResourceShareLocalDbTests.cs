namespace NOIR.IntegrationTests.Persistence;

/// <summary>
/// Integration tests for ResourceShare entity with SQL Server LocalDB.
/// Tests CRUD operations, specifications, and tenant isolation.
/// </summary>
[Collection("LocalDb")]
public class ResourceShareLocalDbTests : IAsyncLifetime
{
    private readonly LocalDbWebApplicationFactory _factory;

    public ResourceShareLocalDbTests(LocalDbWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    #region CRUD Tests

    [Fact]
    public async Task Add_ResourceShare_ShouldPersistToSqlServer()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            var share = ResourceShare.Create(
                "document",
                Guid.NewGuid(),
                "user-123",
                SharePermission.Edit,
                "owner-456");

            // Act
            context.ResourceShares.Add(share);
            await context.SaveChangesAsync();

            // Assert
            var retrieved = await context.ResourceShares.FindAsync(share.Id);
            retrieved.Should().NotBeNull();
            retrieved!.ResourceType.Should().Be("document");
            retrieved.SharedWithUserId.Should().Be("user-123");
            retrieved.Permission.Should().Be(SharePermission.Edit);
        });
    }

    [Fact]
    public async Task Add_ResourceShare_ShouldSetTenantId()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            var share = ResourceShare.Create(
                "document",
                Guid.NewGuid(),
                "user-123",
                SharePermission.View);

            context.ResourceShares.Add(share);
            await context.SaveChangesAsync();

            // Assert
            var retrieved = await context.ResourceShares.FindAsync(share.Id);
            retrieved.Should().NotBeNull();
            retrieved!.TenantId.Should().Be("default");
        }, "default");
    }

    [Fact]
    public async Task Update_ResourceShare_ShouldPersistChanges()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            var share = ResourceShare.Create(
                "document",
                Guid.NewGuid(),
                "user-123",
                SharePermission.View);

            context.ResourceShares.Add(share);
            await context.SaveChangesAsync();

            // Act - Update permission
            share.UpdatePermission(SharePermission.Admin);
            await context.SaveChangesAsync();

            // Assert
            var retrieved = await context.ResourceShares.FindAsync(share.Id);
            retrieved!.Permission.Should().Be(SharePermission.Admin);
        });
    }

    [Fact]
    public async Task Delete_ResourceShare_ShouldSoftDelete()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            var share = ResourceShare.Create(
                "document",
                Guid.NewGuid(),
                "user-123",
                SharePermission.View);

            context.ResourceShares.Add(share);
            await context.SaveChangesAsync();

            // Act - Soft delete
            share.IsDeleted = true;
            share.DeletedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync();

            // Clear change tracker to get fresh query results
            context.ChangeTracker.Clear();

            // Assert - Query with filter should not find it
            var notFound = await context.ResourceShares.FindAsync(share.Id);
            notFound.Should().BeNull();

            // But it should still exist without filter
            var stillExists = await context.ResourceShares
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(rs => rs.Id == share.Id);
            stillExists.Should().NotBeNull();
            stillExists!.IsDeleted.Should().BeTrue();
        });
    }

    #endregion

    #region Specification Tests

    [Fact]
    public async Task ResourceShareByUserSpec_ShouldFindMatchingShare()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();
            var resourceId = Guid.NewGuid();

            var share = ResourceShare.Create(
                "document",
                resourceId,
                "user-123",
                SharePermission.Edit);

            context.ResourceShares.Add(share);
            await context.SaveChangesAsync();

            // Act
            var spec = new ResourceShareByUserSpec("document", resourceId, "user-123");
            var result = await SpecificationEvaluator
                .GetQuery(context.ResourceShares, spec)
                .FirstOrDefaultAsync();

            // Assert
            result.Should().NotBeNull();
            result!.Permission.Should().Be(SharePermission.Edit);
        });
    }

    [Fact]
    public async Task ResourceShareByUserSpec_ShouldNotFindExpiredShare()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();
            var resourceId = Guid.NewGuid();

            var share = ResourceShare.Create(
                "document",
                resourceId,
                "user-123",
                SharePermission.Edit,
                expiresAt: DateTimeOffset.UtcNow.AddHours(-1)); // Expired

            context.ResourceShares.Add(share);
            await context.SaveChangesAsync();

            // Act
            var spec = new ResourceShareByUserSpec("document", resourceId, "user-123");
            var result = await SpecificationEvaluator
                .GetQuery(context.ResourceShares, spec)
                .FirstOrDefaultAsync();

            // Assert - Should not find expired share
            result.Should().BeNull();
        });
    }

    [Fact]
    public async Task ResourceSharesByResourceSpec_ShouldReturnAllSharesForResource()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();
            var resourceId = Guid.NewGuid();

            // Create multiple shares for same resource
            var share1 = ResourceShare.Create("document", resourceId, "user-1", SharePermission.View);
            var share2 = ResourceShare.Create("document", resourceId, "user-2", SharePermission.Edit);
            var share3 = ResourceShare.Create("document", resourceId, "user-3", SharePermission.Admin);

            context.ResourceShares.AddRange(share1, share2, share3);
            await context.SaveChangesAsync();

            // Act
            var spec = new ResourceSharesByResourceSpec("document", resourceId);
            var results = await SpecificationEvaluator
                .GetQuery(context.ResourceShares, spec)
                .ToListAsync();

            // Assert
            results.Should().HaveCount(3);
            results.Select(r => r.SharedWithUserId).Should().BeEquivalentTo(["user-1", "user-2", "user-3"]);
        });
    }

    [Fact]
    public async Task ResourceSharesByUserSpec_ShouldReturnAllSharesForUser()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();
            var userId = "shared-user";

            // Create shares on different resources
            var share1 = ResourceShare.Create("document", Guid.NewGuid(), userId, SharePermission.View);
            var share2 = ResourceShare.Create("folder", Guid.NewGuid(), userId, SharePermission.Edit);
            var share3 = ResourceShare.Create("report", Guid.NewGuid(), userId, SharePermission.Admin);

            context.ResourceShares.AddRange(share1, share2, share3);
            await context.SaveChangesAsync();

            // Act
            var spec = new ResourceSharesByUserSpec(userId);
            var results = await SpecificationEvaluator
                .GetQuery(context.ResourceShares, spec)
                .ToListAsync();

            // Assert
            results.Should().HaveCount(3);
            results.Select(r => r.ResourceType).Should().BeEquivalentTo(["document", "folder", "report"]);
        });
    }

    [Fact]
    public async Task ResourceSharesByUserSpec_WithResourceTypeFilter_ShouldFilterByType()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();
            var userId = "type-filter-user";

            // Create shares on different resource types
            var share1 = ResourceShare.Create("document", Guid.NewGuid(), userId, SharePermission.View);
            var share2 = ResourceShare.Create("document", Guid.NewGuid(), userId, SharePermission.Edit);
            var share3 = ResourceShare.Create("folder", Guid.NewGuid(), userId, SharePermission.Admin);

            context.ResourceShares.AddRange(share1, share2, share3);
            await context.SaveChangesAsync();

            // Act - Filter by document type only
            var spec = new ResourceSharesByUserSpec(userId, "document");
            var results = await SpecificationEvaluator
                .GetQuery(context.ResourceShares, spec)
                .ToListAsync();

            // Assert
            results.Should().HaveCount(2);
            results.Should().OnlyContain(r => r.ResourceType == "document");
        });
    }

    #endregion

    #region Unique Constraint Tests

    [Fact]
    public async Task Add_DuplicateShare_ShouldThrowException()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();
            var resourceId = Guid.NewGuid();

            var share1 = ResourceShare.Create("document", resourceId, "user-123", SharePermission.View);
            context.ResourceShares.Add(share1);
            await context.SaveChangesAsync();

            // Act - Try to add duplicate share
            var share2 = ResourceShare.Create("document", resourceId, "user-123", SharePermission.Edit);
            context.ResourceShares.Add(share2);

            // Assert
            var act = async () => await context.SaveChangesAsync();
            await act.Should().ThrowAsync<DbUpdateException>();
        });
    }

    #endregion

    #region Audit Tracking Tests

    [Fact]
    public async Task Add_ResourceShare_ShouldSetCreatedAt()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();
            var beforeCreate = DateTimeOffset.UtcNow;

            var share = ResourceShare.Create(
                "document",
                Guid.NewGuid(),
                "user-123",
                SharePermission.View);

            context.ResourceShares.Add(share);
            await context.SaveChangesAsync();

            // Assert
            var retrieved = await context.ResourceShares.FindAsync(share.Id);
            retrieved!.CreatedAt.Should().BeAfter(beforeCreate.AddSeconds(-1));
            retrieved.CreatedAt.Should().BeBefore(DateTimeOffset.UtcNow.AddSeconds(1));
        });
    }

    #endregion
}
