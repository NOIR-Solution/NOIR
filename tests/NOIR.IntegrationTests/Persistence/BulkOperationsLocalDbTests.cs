namespace NOIR.IntegrationTests.Persistence;

/// <summary>
/// Integration tests for bulk operations using SQL Server LocalDB.
/// Tests verify that bulk insert, update, delete, sync, and read operations
/// work correctly with a real database using EFCore.BulkExtensions.
///
/// NOTE: EFCore.BulkExtensions requires a real relational database -
/// it does NOT work with InMemory provider.
///
/// IMPORTANT: Bulk operations bypass EF Core interceptors, so TenantId must be
/// set explicitly when creating entities. Use the test helper methods:
/// - CreateBulkTestTokens(count, prefix) for multiple entities
/// - CreateBulkTestToken(userId) for a single entity
///
/// These tests use ApplicationDbContext directly to verify EFCore.BulkExtensions
/// compatibility. The Repository layer adds additional validation (tenant checks,
/// config validation) which is tested separately in unit tests.
/// </summary>
[Collection("LocalDb")]
[Trait("Category", "Integration")]
[Trait("Dependency", "EFCore.BulkExtensions")]
public class BulkOperationsLocalDbTests : IAsyncLifetime
{
    private readonly LocalDbWebApplicationFactory _factory;
    private const string DefaultTenantId = "default";

    public BulkOperationsLocalDbTests(LocalDbWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private static string GenerateTestToken() => Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

    #region Test Helpers

    /// <summary>
    /// Creates a list of refresh tokens for bulk operation testing with proper tenant context.
    /// </summary>
    private static List<RefreshToken> CreateBulkTestTokens(
        int count,
        string userIdPrefix = "test-user",
        int expirationDays = 7)
    {
        return Enumerable.Range(1, count)
            .Select(i => RefreshToken.Create(
                GenerateTestToken(),
                $"{userIdPrefix}-{i}",
                expirationDays,
                DefaultTenantId))
            .ToList();
    }

    /// <summary>
    /// Creates a single refresh token for bulk operation testing with proper tenant context.
    /// </summary>
    private static RefreshToken CreateBulkTestToken(
        string userId,
        int expirationDays = 7)
    {
        return RefreshToken.Create(GenerateTestToken(), userId, expirationDays, DefaultTenantId);
    }

    #endregion

    #region BulkInsertAsync Tests

    [Fact]
    public async Task BulkInsertAsync_ShouldInsertMultipleEntities()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            var tokens = CreateBulkTestTokens(100, "bulk-insert-user");

            // Act
            await context.BulkInsertAsync(tokens);

            // Assert
            var count = await context.RefreshTokens.CountAsync();
            count.Should().Be(100);
        });
    }

    [Fact]
    public async Task BulkInsertAsync_WithOutputIdentity_ShouldReturnGeneratedIds()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            var tokens = CreateBulkTestTokens(10, "output-id-user");

            var config = new BulkConfig { SetOutputIdentity = true, PreserveInsertOrder = true };

            // Act
            await context.BulkInsertAsync(tokens, config);

            // Assert - All tokens should have non-empty IDs
            tokens.Should().AllSatisfy(t => t.Id.Should().NotBe(Guid.Empty));
        });
    }

    [Fact]
    public async Task BulkInsertAsync_EmptyCollection_ShouldNotThrow()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            // Act
            var act = async () => await context.BulkInsertAsync(new List<RefreshToken>());

            // Assert
            await act.Should().NotThrowAsync();
        });
    }

    #endregion

    #region BulkUpdateAsync Tests

    [Fact]
    public async Task BulkUpdateAsync_ShouldUpdateExistingEntities()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            // Setup - Insert initial data
            var tokens = CreateBulkTestTokens(10, "update-user");

            await context.BulkInsertAsync(tokens);
            context.ChangeTracker.Clear();

            // Act - Revoke all tokens
            foreach (var token in tokens)
            {
                token.Revoke("192.168.1.1", "Bulk revoke test");
            }
            await context.BulkUpdateAsync(tokens);

            // Assert
            context.ChangeTracker.Clear();
            var updated = await context.RefreshTokens
                .Where(t => t.UserId.StartsWith("update-user-"))
                .ToListAsync();

            updated.Should().AllSatisfy(t =>
            {
                t.IsRevoked.Should().BeTrue();
                t.RevokedByIp.Should().Be("192.168.1.1");
            });
        });
    }

    #endregion

    #region BulkInsertOrUpdateAsync Tests

    [Fact]
    public async Task BulkInsertOrUpdateAsync_ShouldInsertNew_UpdateExisting()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            // Setup - Insert initial data
            var existingToken = CreateBulkTestToken("upsert-existing-user");
            await context.BulkInsertAsync(new[] { existingToken });
            context.ChangeTracker.Clear();

            // Act - Upsert: update existing + insert new
            existingToken.Revoke("10.0.0.1", "Upsert update");
            var newToken = CreateBulkTestToken("upsert-new-user", 14);

            await context.BulkInsertOrUpdateAsync(new[] { existingToken, newToken });

            // Assert
            context.ChangeTracker.Clear();
            var count = await context.RefreshTokens.CountAsync();
            count.Should().Be(2);

            var updated = await context.RefreshTokens.FindAsync(existingToken.Id);
            updated!.IsRevoked.Should().BeTrue();

            var inserted = await context.RefreshTokens
                .FirstOrDefaultAsync(t => t.UserId == "upsert-new-user");
            inserted.Should().NotBeNull();
        });
    }

    #endregion

    #region BulkDeleteAsync Tests

    [Fact]
    public async Task BulkDeleteAsync_ShouldHardDeleteEntities()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            // Setup
            var tokens = CreateBulkTestTokens(5, "delete-user");

            await context.BulkInsertAsync(tokens);
            context.ChangeTracker.Clear();

            var countBefore = await context.RefreshTokens
                .Where(t => t.UserId.StartsWith("delete-user-"))
                .CountAsync();
            countBefore.Should().Be(5);

            // Act - Delete 3 of them
            var tokensToDelete = tokens.Take(3).ToList();
            await context.BulkDeleteAsync(tokensToDelete);

            // Assert - Hard delete (not soft delete)
            context.ChangeTracker.Clear();
            var countAfter = await context.RefreshTokens
                .Where(t => t.UserId.StartsWith("delete-user-"))
                .CountAsync();
            countAfter.Should().Be(2);

            // Verify they're truly gone (not soft deleted)
            var deletedCount = await context.RefreshTokens
                .IgnoreQueryFilters()
                .CountAsync(t => tokensToDelete.Select(d => d.Id).Contains(t.Id));
            deletedCount.Should().Be(0, "bulk delete should hard delete, not soft delete");
        });
    }

    #endregion

    #region BulkReadAsync Tests

    [Fact]
    public async Task BulkReadAsync_ShouldReadByKeys()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            // Setup
            var tokens = CreateBulkTestTokens(10, "read-user");

            await context.BulkInsertAsync(tokens);
            context.ChangeTracker.Clear();

            // Create lookup stubs with just IDs
            var lookups = tokens.Take(5).Select(t =>
            {
                var stub = CreateBulkTestToken("placeholder", 1);
                // Use reflection to set the ID since it's protected
                typeof(Entity<Guid>)
                    .GetProperty("Id")!
                    .SetValue(stub, t.Id);
                return stub;
            }).ToList();

            // Act
            await context.BulkReadAsync(lookups);

            // Assert
            lookups.Should().HaveCount(5);
            lookups.Should().AllSatisfy(r => r.UserId.Should().StartWith("read-user-"));
        });
    }

    #endregion

    #region Transaction Tests

    [Fact]
    public async Task BulkOperations_InTransaction_ShouldRollbackOnFailure()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            await using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                // Insert some tokens in transaction
                var tokens = CreateBulkTestTokens(10, "tx-user");

                await context.BulkInsertAsync(tokens);

                // Verify they exist in transaction context
                var countInTx = await context.RefreshTokens.CountAsync();
                countInTx.Should().Be(10);

                // Rollback
                await transaction.RollbackAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            // After rollback, data should be gone
            context.ChangeTracker.Clear();
            var countAfter = await context.RefreshTokens.CountAsync();
            countAfter.Should().Be(0);
        });
    }

    #endregion

    #region Performance Measurement Tests

    [Fact]
    public async Task BulkInsertAsync_ShouldBeFasterThanAddRange()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            const int recordCount = 500;

            // Measure bulk insert
            var bulkTokens = CreateBulkTestTokens(recordCount, "bulk-perf");

            var bulkStopwatch = System.Diagnostics.Stopwatch.StartNew();
            await context.BulkInsertAsync(bulkTokens);
            bulkStopwatch.Stop();

            var bulkDuration = bulkStopwatch.Elapsed;

            // Reset database
            await _factory.ResetDatabaseAsync();

            // Measure standard AddRange
            var standardTokens = CreateBulkTestTokens(recordCount, "standard-perf");

            var standardStopwatch = System.Diagnostics.Stopwatch.StartNew();
            context.RefreshTokens.AddRange(standardTokens);
            await context.SaveChangesAsync();
            standardStopwatch.Stop();

            var standardDuration = standardStopwatch.Elapsed;

            // Assert - Bulk should be significantly faster
            // (At least 2x faster for 500 records, typically much more)
            bulkDuration.Should().BeLessThan(standardDuration,
                $"Bulk insert ({bulkDuration.TotalMilliseconds}ms) should be faster than " +
                $"AddRange ({standardDuration.TotalMilliseconds}ms)");
        });
    }

    #endregion
}
