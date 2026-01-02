namespace NOIR.IntegrationTests.Persistence;

/// <summary>
/// Integration tests for Repository pattern through DbContext.
/// Tests all repository methods with SQL Server LocalDB.
/// </summary>
[Collection("LocalDb")]
public class RepositoryPatternTests : IAsyncLifetime
{
    private readonly LocalDbWebApplicationFactory _factory;

    public RepositoryPatternTests(LocalDbWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetById_WithExistingId_ShouldReturnEntity()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            var token = RefreshToken.Create("test-user", 7);
            context.RefreshTokens.Add(token);
            await context.SaveChangesAsync();

            // Act
            var result = await context.RefreshTokens.FindAsync(token.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(token.Id);
            result.UserId.Should().Be("test-user");
        });
    }

    [Fact]
    public async Task GetById_WithNonExistingId_ShouldReturnNull()
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

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAll_ShouldReturnAllEntities()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            context.RefreshTokens.Add(RefreshToken.Create("user-1", 7));
            context.RefreshTokens.Add(RefreshToken.Create("user-2", 7));
            context.RefreshTokens.Add(RefreshToken.Create("user-3", 7));
            await context.SaveChangesAsync();

            // Act
            var result = await context.RefreshTokens.AsNoTracking().ToListAsync();

            // Assert
            result.Should().HaveCount(3);
        });
    }

    #endregion

    #region ExistsAsync Tests

    [Fact]
    public async Task Exists_WithExistingId_ShouldReturnTrue()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            var token = RefreshToken.Create("exists-user", 7);
            context.RefreshTokens.Add(token);
            await context.SaveChangesAsync();

            // Act
            var exists = await context.RefreshTokens.AnyAsync(t => t.Id == token.Id);

            // Assert
            exists.Should().BeTrue();
        });
    }

    [Fact]
    public async Task Exists_WithNonExistingId_ShouldReturnFalse()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            // Act
            var exists = await context.RefreshTokens.AnyAsync(t => t.Id == Guid.NewGuid());

            // Assert
            exists.Should().BeFalse();
        });
    }

    #endregion

    #region CountAsync Tests

    [Fact]
    public async Task Count_ShouldReturnCorrectCount()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            var initialCount = await context.RefreshTokens.CountAsync();

            context.RefreshTokens.Add(RefreshToken.Create("count-1", 7));
            context.RefreshTokens.Add(RefreshToken.Create("count-2", 7));
            await context.SaveChangesAsync();

            // Act
            var count = await context.RefreshTokens.CountAsync();

            // Assert
            count.Should().Be(initialCount + 2);
        });
    }

    [Fact]
    public async Task Count_WithPredicate_ShouldReturnFilteredCount()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            var userId = $"count-predicate-{Guid.NewGuid()}";
            context.RefreshTokens.Add(RefreshToken.Create(userId, 7));
            context.RefreshTokens.Add(RefreshToken.Create(userId, 7));
            context.RefreshTokens.Add(RefreshToken.Create("other-user", 7));
            await context.SaveChangesAsync();

            // Act
            var count = await context.RefreshTokens.CountAsync(t => t.UserId == userId);

            // Assert
            count.Should().Be(2);
        });
    }

    #endregion

    #region FirstOrDefaultAsync Tests

    [Fact]
    public async Task FirstOrDefault_WithMatchingPredicate_ShouldReturnEntity()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            var userId = $"first-{Guid.NewGuid()}";
            var token = RefreshToken.Create(userId, 7);
            context.RefreshTokens.Add(token);
            await context.SaveChangesAsync();

            // Act
            var result = await context.RefreshTokens
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == userId);

            // Assert
            result.Should().NotBeNull();
            result!.UserId.Should().Be(userId);
        });
    }

    [Fact]
    public async Task FirstOrDefault_WithNoMatch_ShouldReturnNull()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            // Act
            var result = await context.RefreshTokens
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == "non-existent-xyz");

            // Assert
            result.Should().BeNull();
        });
    }

    #endregion

    #region SingleOrDefaultAsync Tests

    [Fact]
    public async Task SingleOrDefault_WithSingleMatch_ShouldReturnEntity()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            var token = RefreshToken.Create($"single-{Guid.NewGuid()}", 7);
            context.RefreshTokens.Add(token);
            await context.SaveChangesAsync();

            // Act
            var result = await context.RefreshTokens
                .AsNoTracking()
                .SingleOrDefaultAsync(t => t.Id == token.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(token.Id);
        });
    }

    #endregion

    #region FindAsync Tests

    [Fact]
    public async Task Find_WithMatchingPredicate_ShouldReturnFilteredList()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            var userId = $"find-{Guid.NewGuid()}";
            context.RefreshTokens.Add(RefreshToken.Create(userId, 7));
            context.RefreshTokens.Add(RefreshToken.Create(userId, 7));
            context.RefreshTokens.Add(RefreshToken.Create("other", 7));
            await context.SaveChangesAsync();

            // Act
            var result = await context.RefreshTokens
                .AsNoTracking()
                .Where(t => t.UserId == userId)
                .ToListAsync();

            // Assert
            result.Should().HaveCount(2);
            result.Should().AllSatisfy(t => t.UserId.Should().Be(userId));
        });
    }

    #endregion

    #region AddAsync Tests

    [Fact]
    public async Task Add_ShouldPersistEntity()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            var token = RefreshToken.Create("add-user", 7);

            // Act
            context.RefreshTokens.Add(token);
            await context.SaveChangesAsync();

            // Assert
            var saved = await context.RefreshTokens.FindAsync(token.Id);
            saved.Should().NotBeNull();
        });
    }

    [Fact]
    public async Task AddRange_ShouldPersistMultipleEntities()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            var tokens = new[]
            {
                RefreshToken.Create("range-1", 7),
                RefreshToken.Create("range-2", 7),
                RefreshToken.Create("range-3", 7)
            };

            // Act
            context.RefreshTokens.AddRange(tokens);
            await context.SaveChangesAsync();

            // Assert
            foreach (var token in tokens)
            {
                var saved = await context.RefreshTokens.FindAsync(token.Id);
                saved.Should().NotBeNull();
            }
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
            token.Revoke("127.0.0.1", "Test reason");
            await context.SaveChangesAsync();

            // Assert
            context.ChangeTracker.Clear();
            var updated = await context.RefreshTokens.FindAsync(token.Id);
            updated!.IsRevoked.Should().BeTrue();
            updated.RevokedByIp.Should().Be("127.0.0.1");
        });
    }

    #endregion

    #region Remove (Soft Delete) Tests

    [Fact]
    public async Task Remove_ShouldSoftDeleteEntity()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            var token = RefreshToken.Create("soft-delete-user", 7);
            context.RefreshTokens.Add(token);
            await context.SaveChangesAsync();
            var tokenId = token.Id;

            // Act
            context.RefreshTokens.Remove(token);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            // Assert - Normal query should not find it
            var normalQuery = await context.RefreshTokens
                .FirstOrDefaultAsync(t => t.Id == tokenId);
            normalQuery.Should().BeNull();

            // But IgnoreQueryFilters should find it with IsDeleted = true
            var withFiltersIgnored = await context.RefreshTokens
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == tokenId);
            withFiltersIgnored.Should().NotBeNull();
            withFiltersIgnored!.IsDeleted.Should().BeTrue();
        });
    }

    #endregion

    #region IgnoreQueryFilters Tests

    [Fact]
    public async Task IgnoreQueryFilters_ShouldIncludeDeletedEntities()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            var token = RefreshToken.Create("filter-test", 7);
            context.RefreshTokens.Add(token);
            await context.SaveChangesAsync();

            context.RefreshTokens.Remove(token);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            // Act
            var deletedIncluded = await context.RefreshTokens
                .IgnoreQueryFilters()
                .Where(t => t.Id == token.Id)
                .ToListAsync();

            // Assert
            deletedIncluded.Should().HaveCount(1);
            deletedIncluded[0].IsDeleted.Should().BeTrue();
        });
    }

    #endregion

    #region AnyAsync Tests

    [Fact]
    public async Task Any_WithNoData_ShouldReturnFalse()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            // Act
            var any = await context.RefreshTokens.AnyAsync(t => t.UserId == "never-exists-xyz");

            // Assert
            any.Should().BeFalse();
        });
    }

    [Fact]
    public async Task Any_WithData_ShouldReturnTrue()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            var token = RefreshToken.Create("any-test", 7);
            context.RefreshTokens.Add(token);
            await context.SaveChangesAsync();

            // Act
            var any = await context.RefreshTokens.AnyAsync(t => t.UserId == "any-test");

            // Assert
            any.Should().BeTrue();
        });
    }

    #endregion
}
