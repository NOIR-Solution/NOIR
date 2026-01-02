namespace NOIR.IntegrationTests.Persistence;

/// <summary>
/// Integration tests for Repository pattern with LocalDB database.
/// Tests CRUD operations and specification evaluation.
/// All operations use ExecuteWithTenantAsync for proper multi-tenant support.
/// </summary>
[Collection("Integration")]
public class RepositoryTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public RepositoryTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        // Clean up test data with tenant context
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            context.RefreshTokens.RemoveRange(context.RefreshTokens);
            context.EntityAuditLogs.RemoveRange(context.EntityAuditLogs);
            await context.SaveChangesAsync();
        });
    }

    #region RefreshToken Repository Tests (via DbContext)

    [Fact]
    public async Task AddAsync_ShouldAddEntityToDatabase()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();

            // Arrange
            var token = RefreshToken.Create("test-user", 7);

            // Act
            context.RefreshTokens.Add(token);
            await context.SaveChangesAsync();

            // Assert
            var savedToken = await context.RefreshTokens.FindAsync(token.Id);
            savedToken.Should().NotBeNull();
            savedToken!.UserId.Should().Be("test-user");
        });
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnEntity()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();

            // Arrange
            var token = RefreshToken.Create("test-user-get", 7);
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
    public async Task GetByIdAsync_WithNonExistentId_ShouldReturnNull()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();

            // Act
            var result = await context.RefreshTokens.FindAsync(Guid.NewGuid());

            // Assert
            result.Should().BeNull();
        });
    }

    [Fact]
    public async Task Update_ShouldModifyEntity()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();

            // Arrange
            var token = RefreshToken.Create("test-user-update", 7);
            context.RefreshTokens.Add(token);
            await context.SaveChangesAsync();

            // Act
            token.Revoke("127.0.0.1", "Test revocation");
            await context.SaveChangesAsync();

            // Assert
            var updatedToken = await context.RefreshTokens.FindAsync(token.Id);
            updatedToken.Should().NotBeNull();
            updatedToken!.IsRevoked.Should().BeTrue();
            updatedToken.RevokedByIp.Should().Be("127.0.0.1");
        });
    }

    [Fact]
    public async Task Remove_ShouldSoftDeleteEntity()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();

            // Arrange
            var token = RefreshToken.Create("test-user-delete", 7);
            context.RefreshTokens.Add(token);
            await context.SaveChangesAsync();
            var tokenId = token.Id;

            // Act
            context.RefreshTokens.Remove(token);
            await context.SaveChangesAsync();

            // Clear change tracker to get fresh query results
            context.ChangeTracker.Clear();

            // Assert - Entity is soft deleted (filtered out by default query)
            var deletedToken = await context.RefreshTokens
                .Where(t => t.Id == tokenId)
                .FirstOrDefaultAsync();
            deletedToken.Should().BeNull("entity should be filtered out by soft delete query filter");

            // But still exists in database with IsDeleted = true
            var softDeleted = await context.RefreshTokens
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == tokenId);
            softDeleted.Should().NotBeNull();
            softDeleted!.IsDeleted.Should().BeTrue();
            softDeleted.DeletedAt.Should().NotBeNull();
        });
    }

    [Fact]
    public async Task Count_ShouldReturnCorrectCount()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();

            // Arrange
            var initialCount = await context.RefreshTokens.CountAsync();
            context.RefreshTokens.Add(RefreshToken.Create("user-1", 7));
            context.RefreshTokens.Add(RefreshToken.Create("user-2", 7));
            await context.SaveChangesAsync();

            // Act
            var count = await context.RefreshTokens.CountAsync();

            // Assert
            count.Should().Be(initialCount + 2);
        });
    }

    [Fact]
    public async Task Any_ShouldReturnTrueWhenExists()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();

            // Arrange
            var token = RefreshToken.Create("any-test-user", 7);
            context.RefreshTokens.Add(token);
            await context.SaveChangesAsync();

            // Act
            var exists = await context.RefreshTokens.AnyAsync(t => t.UserId == "any-test-user");

            // Assert
            exists.Should().BeTrue();
        });
    }

    [Fact]
    public async Task Any_ShouldReturnFalseWhenNotExists()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();

            // Act
            var exists = await context.RefreshTokens.AnyAsync(t => t.UserId == "non-existent-user-xyz");

            // Assert
            exists.Should().BeFalse();
        });
    }

    #endregion

    #region EntityAuditLog Repository Tests

    [Fact]
    public async Task EntityAuditLog_AddAndRetrieve_ShouldWork()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var unitOfWork = services.GetRequiredService<IUnitOfWork>();

            // Arrange
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
            saved.EntityType.Should().Be("TestEntity");
        });
    }

    [Fact]
    public async Task EntityAuditLog_QueryByEntityType_ShouldReturnMatchingLogs()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var unitOfWork = services.GetRequiredService<IUnitOfWork>();

            // Arrange
            var correlationId = Guid.NewGuid().ToString();
            context.EntityAuditLogs.Add(EntityAuditLog.Create(correlationId, "Customer", "1", EntityAuditOperation.Added, null, null, null));
            context.EntityAuditLogs.Add(EntityAuditLog.Create(correlationId, "Customer", "2", EntityAuditOperation.Modified, null, null, null));
            context.EntityAuditLogs.Add(EntityAuditLog.Create(correlationId, "Order", "3", EntityAuditOperation.Added, null, null, null));
            await unitOfWork.SaveChangesAsync();

            // Act
            var customerLogs = await context.EntityAuditLogs
                .Where(a => a.EntityType == "Customer")
                .ToListAsync();

            // Assert
            customerLogs.Should().HaveCount(2);
        });
    }

    #endregion
}
