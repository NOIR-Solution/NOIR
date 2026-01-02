namespace NOIR.IntegrationTests.Persistence;

/// <summary>
/// Integration tests for Repository pattern using SQL Server LocalDB.
/// Tests CRUD operations, specifications, and complex queries with real SQL Server.
/// </summary>
[Collection("LocalDb")]
public class RepositoryLocalDbTests : IAsyncLifetime
{
    private readonly LocalDbWebApplicationFactory _factory;

    public RepositoryLocalDbTests(LocalDbWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    #region RefreshToken CRUD Tests via DbContext

    [Fact]
    public async Task Add_RefreshToken_ShouldPersistToSqlServer()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            var token = RefreshToken.Create("sql-user", 7);

            // Act
            context.RefreshTokens.Add(token);
            await context.SaveChangesAsync();

            // Assert - Query should work with SQL Server
            var retrieved = await context.RefreshTokens.FindAsync(token.Id);
            retrieved.Should().NotBeNull();
            retrieved!.UserId.Should().Be("sql-user");
        });
    }

    [Fact]
    public async Task AddRange_RefreshTokens_ShouldPersistMultipleEntities()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            var tokens = new[]
            {
                RefreshToken.Create("batch-user-1", 7),
                RefreshToken.Create("batch-user-2", 7),
                RefreshToken.Create("batch-user-3", 7)
            };

            // Act
            context.RefreshTokens.AddRange(tokens);
            await context.SaveChangesAsync();

            // Assert
            var count = await context.RefreshTokens.CountAsync();
            count.Should().Be(3);
        });
    }

    #endregion

    #region Query Tests

    [Fact]
    public async Task GetById_ExistingEntity_ShouldReturnEntity()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            var token = RefreshToken.Create("get-test-user", 7);
            context.RefreshTokens.Add(token);
            await context.SaveChangesAsync();

            // Act
            var result = await context.RefreshTokens.FindAsync(token.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(token.Id);
        });
    }

    [Fact]
    public async Task GetById_NonExistingEntity_ShouldReturnNull()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            // Act
            var result = await context.RefreshTokens.FindAsync(Guid.NewGuid());

            // Assert
            result.Should().BeNull();
        });
    }

    #endregion

    #region LINQ Query Tests

    [Fact]
    public async Task Where_WithFilter_ShouldReturnFilteredResults()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            // Add tokens for different users
            var tokens = new[]
            {
                RefreshToken.Create("filter-user-1", 7),
                RefreshToken.Create("filter-user-1", 7),
                RefreshToken.Create("filter-user-2", 7)
            };
            context.RefreshTokens.AddRange(tokens);
            await context.SaveChangesAsync();

            // Act
            // Note: IsRevoked is a computed property, use RevokedAt.HasValue for DB queries
            var result = await context.RefreshTokens
                .Where(t => t.UserId == "filter-user-1" && !t.RevokedAt.HasValue)
                .ToListAsync();

            // Assert
            result.Should().HaveCount(2);
            result.Should().AllSatisfy(t => t.UserId.Should().Be("filter-user-1"));
        });
    }

    [Fact]
    public async Task Count_WithFilter_ShouldReturnCorrectCount()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            var tokens = new[]
            {
                RefreshToken.Create("count-user", 7),
                RefreshToken.Create("count-user", 7),
                RefreshToken.Create("other-user", 7)
            };
            context.RefreshTokens.AddRange(tokens);
            await context.SaveChangesAsync();

            // Act
            var count = await context.RefreshTokens
                .CountAsync(t => t.UserId == "count-user");

            // Assert
            count.Should().Be(2);
        });
    }

    [Fact]
    public async Task Any_WithFilter_ShouldWork()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            var token = RefreshToken.Create("any-user", 7);
            context.RefreshTokens.Add(token);
            await context.SaveChangesAsync();

            // Act
            var exists = await context.RefreshTokens
                .AnyAsync(t => t.Token == token.Token);

            // Assert
            exists.Should().BeTrue();
        });
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ShouldPersistChanges()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            var token = RefreshToken.Create("update-user", 7);
            context.RefreshTokens.Add(token);
            await context.SaveChangesAsync();

            // Act
            token.Revoke("127.0.0.1", "Test revocation");
            await context.SaveChangesAsync();

            // Assert - Get fresh from database
            var updated = await context.RefreshTokens.FindAsync(token.Id);
            updated!.IsRevoked.Should().BeTrue();
            updated.RevokedByIp.Should().Be("127.0.0.1");
        });
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_ShouldSoftDeleteEntity()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            var token = RefreshToken.Create("delete-user", 7);
            context.RefreshTokens.Add(token);
            await context.SaveChangesAsync();
            var tokenId = token.Id;

            // Act
            context.RefreshTokens.Remove(token);
            await context.SaveChangesAsync();

            // Clear change tracker to get fresh query results
            context.ChangeTracker.Clear();

            // Assert - Entity is soft deleted (filtered out by default query)
            var deleted = await context.RefreshTokens
                .Where(t => t.Id == tokenId)
                .FirstOrDefaultAsync();
            deleted.Should().BeNull("entity should be filtered out by soft delete query filter");

            // But still exists in database with IsDeleted = true
            var softDeleted = await context.RefreshTokens
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == tokenId);
            softDeleted.Should().NotBeNull();
            softDeleted!.IsDeleted.Should().BeTrue();
            softDeleted.DeletedAt.Should().NotBeNull();
        });
    }

    #endregion

    #region EntityAuditLog Tests

    [Fact]
    public async Task EntityAuditLog_AddAndRetrieve_ShouldWorkWithSqlServer()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();

            var auditLog = EntityAuditLog.Create(
                correlationId: Guid.NewGuid().ToString(),
                entityType: "TestEntity",
                entityId: "123",
                operation: EntityAuditOperation.Added,
                entityDiff: null,
                tenantId: null,
                handlerAuditLogId: null);

            // Act
            context.EntityAuditLogs.Add(auditLog);
            await unitOfWork.SaveChangesAsync();

            // Assert
            var saved = await context.EntityAuditLogs.FindAsync(auditLog.Id);
            saved.Should().NotBeNull();
            saved!.Operation.Should().Be(nameof(EntityAuditOperation.Added));
        });
    }

    [Fact]
    public async Task EntityAuditLog_ShouldHandleLargeJsonValues()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();

            var largeJson = new string('x', 50000);

            var auditLog = EntityAuditLog.Create(
                correlationId: Guid.NewGuid().ToString(),
                entityType: "TestEntity",
                entityId: "123",
                operation: EntityAuditOperation.Added,
                entityDiff: largeJson,
                tenantId: null,
                handlerAuditLogId: null);

            context.EntityAuditLogs.Add(auditLog);
            await unitOfWork.SaveChangesAsync();

            // Assert
            var saved = await context.EntityAuditLogs.FindAsync(auditLog.Id);
            saved!.EntityDiff.Should().HaveLength(50000);
        });
    }

    #endregion

    #region SQL Server Specific Tests

    [Fact]
    public async Task SqlServer_ShouldSupportConcurrentReads()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            // Setup
            var tokens = Enumerable.Range(1, 10)
                .Select(i => RefreshToken.Create($"concurrent-{i}", 7))
                .ToList();
            context.RefreshTokens.AddRange(tokens);
            await context.SaveChangesAsync();

            // Act - Parallel reads
            var tasks = Enumerable.Range(1, 5).Select(async _ =>
            {
                await _factory.ExecuteWithTenantAsync(async innerSp =>
                {
                    var innerContext = innerSp.GetRequiredService<ApplicationDbContext>();
                    await innerContext.RefreshTokens.CountAsync();
                });
            });

            // Assert - Should complete without errors
            await Task.WhenAll(tasks);
        });
    }

    [Fact]
    public async Task SqlServer_OrderBy_ShouldWorkCorrectly()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            var tokens = new[]
            {
                RefreshToken.Create("z-user", 7),
                RefreshToken.Create("a-user", 7),
                RefreshToken.Create("m-user", 7)
            };
            context.RefreshTokens.AddRange(tokens);
            await context.SaveChangesAsync();

            // Act
            var ordered = await context.RefreshTokens
                .OrderBy(t => t.UserId)
                .Select(t => t.UserId)
                .ToListAsync();

            // Assert
            ordered.Should().BeInAscendingOrder();
        });
    }

    #endregion
}
