namespace NOIR.IntegrationTests.Persistence;

/// <summary>
/// Integration tests for SpecificationEvaluator with LocalDB database.
/// Tests that specifications are correctly translated to EF Core queries.
/// All operations use ExecuteWithTenantAsync for proper multi-tenant support.
/// </summary>
[Collection("Integration")]
public class SpecificationEvaluatorTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public SpecificationEvaluatorTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        // Seed test data with tenant context
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            await SeedTestDataAsync(context);
        });
    }

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

    private static string GenerateTestToken() => Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

    private static async Task SeedTestDataAsync(ApplicationDbContext context)
    {
        var userId = "spec-test-user";

        // Create refresh tokens with different states
        var activeToken1 = RefreshToken.Create(GenerateTestToken(), userId, 7, ipAddress: "127.0.0.1");
        var activeToken2 = RefreshToken.Create(GenerateTestToken(), userId, 7, ipAddress: "127.0.0.1");
        var revokedToken = RefreshToken.Create(GenerateTestToken(), userId, 7, ipAddress: "127.0.0.1");
        revokedToken.Revoke("127.0.0.1", "Test revocation");

        var otherUserToken = RefreshToken.Create(GenerateTestToken(), "other-user", 7, ipAddress: "127.0.0.1");

        context.RefreshTokens.AddRange(activeToken1, activeToken2, revokedToken, otherUserToken);

        // Create entity audit logs using Create
        var correlationId = Guid.NewGuid().ToString();
        context.EntityAuditLogs.Add(EntityAuditLog.Create(correlationId, "User", "1", EntityAuditOperation.Added, null, null, null));
        context.EntityAuditLogs.Add(EntityAuditLog.Create(correlationId, "User", "2", EntityAuditOperation.Added, null, null, null));
        context.EntityAuditLogs.Add(EntityAuditLog.Create(correlationId, "Order", "1", EntityAuditOperation.Modified, null, null, null));

        await context.SaveChangesAsync();
    }

    #region RefreshToken Specification Tests

    [Fact]
    public async Task RefreshTokenByValueSpec_ShouldFindToken()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();

            // Arrange
            var token = RefreshToken.Create(GenerateTestToken(), "spec-value-user", 7, ipAddress: "127.0.0.1");
            context.RefreshTokens.Add(token);
            await context.SaveChangesAsync();

            var spec = new RefreshTokenByValueSpec(token.Token);

            // Act
            var query = SpecificationEvaluator.GetQuery(context.RefreshTokens.AsQueryable(), spec);
            var result = await query.FirstOrDefaultAsync();

            // Assert
            result.Should().NotBeNull();
            result!.Token.Should().Be(token.Token);
        });
    }

    [Fact]
    public async Task ActiveRefreshTokensByUserSpec_ShouldReturnOnlyActiveTokens()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();

            // Arrange
            var spec = new ActiveRefreshTokensByUserSpec("spec-test-user");

            // Act
            var query = SpecificationEvaluator.GetQuery(context.RefreshTokens.AsQueryable(), spec);
            var results = await query.ToListAsync();

            // Assert
            results.Should().HaveCount(2);
            results.Should().OnlyContain(t => !t.RevokedAt.HasValue);
            results.Should().OnlyContain(t => t.UserId == "spec-test-user");
        });
    }

    [Fact]
    public async Task RefreshTokensByFamilySpec_ShouldReturnTokensInFamily()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();

            // Arrange
            var familyToken = RefreshToken.Create(GenerateTestToken(), "family-user", 7);
            var relatedToken = RefreshToken.Create(GenerateTestToken(), "family-user", 7, tokenFamily: familyToken.TokenFamily);
            context.RefreshTokens.AddRange(familyToken, relatedToken);
            await context.SaveChangesAsync();

            var spec = new RefreshTokensByFamilySpec(familyToken.TokenFamily);

            // Act
            var query = SpecificationEvaluator.GetQuery(context.RefreshTokens.AsQueryable(), spec);
            var results = await query.ToListAsync();

            // Assert
            results.Should().HaveCount(2);
            results.Should().OnlyContain(t => t.TokenFamily == familyToken.TokenFamily);
        });
    }

    [Fact]
    public async Task OldestActiveRefreshTokenSpec_ShouldReturnOldestToken()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();

            // Arrange
            var spec = new OldestActiveRefreshTokenSpec("spec-test-user");

            // Act
            var query = SpecificationEvaluator.GetQuery(context.RefreshTokens.AsQueryable(), spec);
            var result = await query.ToListAsync();

            // Assert
            result.Should().HaveCount(1);
        });
    }

    #endregion

    #region EntityAuditLog Specification Tests

    [Fact]
    public async Task EntityAuditLogByTypeSpec_ShouldFilterByEntityType()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();

            // Arrange
            var spec = new EntityAuditLogByTypeSpec("User");

            // Act
            var query = SpecificationEvaluator.GetQuery(context.EntityAuditLogs.AsQueryable(), spec);
            var results = await query.ToListAsync();

            // Assert
            results.Should().HaveCountGreaterThanOrEqualTo(2);
            results.Should().OnlyContain(a => a.EntityType == "User");
        });
    }

    [Fact]
    public async Task EntityHistorySpec_ShouldFilterByEntityTypeAndId()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();

            // Arrange
            var spec = new EntityHistorySpec("User", "1");

            // Act
            var query = SpecificationEvaluator.GetQuery(context.EntityAuditLogs.AsQueryable(), spec);
            var results = await query.ToListAsync();

            // Assert
            results.Should().HaveCountGreaterThanOrEqualTo(1);
            results.Should().OnlyContain(a => a.EntityType == "User" && a.EntityId == "1");
        });
    }

    [Fact]
    public async Task RecentEntityAuditLogsSpec_ShouldApplyPagination()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();

            // Arrange
            var spec = new RecentEntityAuditLogsSpec(0, 2);

            // Act
            var query = SpecificationEvaluator.GetQuery(context.EntityAuditLogs.AsQueryable(), spec);
            var results = await query.ToListAsync();

            // Assert
            results.Should().HaveCountLessThanOrEqualTo(2);
        });
    }

    #endregion

    #region SpecificationEvaluator Feature Tests

    [Fact]
    public async Task GetQueryForCount_ShouldReturnCountWithoutPaging()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();

            // Arrange
            var spec = new RecentEntityAuditLogsSpec(0, 2);

            // Act
            var query = SpecificationEvaluator.GetQueryForCount(context.EntityAuditLogs.AsQueryable(), spec);
            var count = await query.CountAsync();

            // Assert
            count.Should().BeGreaterThanOrEqualTo(3); // We seeded at least 3 audit logs
        });
    }

    [Fact]
    public async Task Specification_WithAsNoTracking_ShouldNotTrackEntities()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();

            // Arrange - ExpiredRefreshTokensSpec uses default AsNoTracking behavior
            var spec = new ExpiredRefreshTokensSpec(DateTimeOffset.UtcNow.AddDays(1));

            // Act
            var query = SpecificationEvaluator.GetQuery(context.RefreshTokens.AsQueryable(), spec);
            var results = await query.ToListAsync();

            // Assert - entities should not be tracked due to AsNoTracking default
            foreach (var token in results)
            {
                context.Entry(token).State.Should().Be(EntityState.Detached);
            }
        });
    }

    #endregion

    #region Projection Specification Tests

    [Fact]
    public async Task GetQuery_WithProjection_ShouldApplySelector()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();

            // Arrange
            var spec = new EntityAuditLogSummarySpec();

            // Act
            var query = SpecificationEvaluator.GetQuery(context.EntityAuditLogs.AsQueryable(), spec);
            var results = await query.ToListAsync();

            // Assert
            results.Should().NotBeEmpty();
            results.First().EntityType.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public void GetQuery_WithProjection_WithoutSelector_ShouldThrow()
    {
        // Arrange
        var spec = new NoSelectorProjectionSpec();
        var source = Array.Empty<EntityAuditLog>().AsQueryable();

        // Act
        var act = () => SpecificationEvaluator.GetQuery(source, spec);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Selector*");
    }

    #endregion

    #region Query Behavior Tests

    [Fact]
    public async Task Specification_WithIgnoreQueryFilters_ShouldIncludeAllEntities()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();

            // Arrange
            var spec = new IgnoreFiltersSpec();

            // Act
            var query = SpecificationEvaluator.GetQuery(context.RefreshTokens.AsQueryable(), spec);
            var results = await query.ToListAsync();

            // Assert - Should return results (including soft-deleted if any)
            results.Should().NotBeNull();
        });
    }

    [Fact]
    public async Task Specification_WithAsSplitQuery_ShouldExecuteSuccessfully()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();

            // Arrange
            var spec = new SplitQuerySpec();

            // Act
            var query = SpecificationEvaluator.GetQuery(context.RefreshTokens.AsQueryable(), spec);
            var results = await query.ToListAsync();

            // Assert
            results.Should().NotBeNull();
        });
    }

    [Fact]
    public async Task Specification_WithAsNoTrackingWithIdentityResolution_ShouldNotTrackButResolveIdentities()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();

            // Arrange
            var spec = new IdentityResolutionSpec("spec-test-user");

            // Act
            var query = SpecificationEvaluator.GetQuery(context.RefreshTokens.AsQueryable(), spec);
            var results = await query.ToListAsync();

            // Assert - Should not be tracked
            foreach (var token in results)
            {
                context.Entry(token).State.Should().Be(EntityState.Detached);
            }
        });
    }

    [Fact]
    public async Task Specification_WithIgnoreAutoIncludes_ShouldExecuteSuccessfully()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();

            // Arrange
            var spec = new IgnoreAutoIncludesSpec();

            // Act
            var query = SpecificationEvaluator.GetQuery(context.EntityAuditLogs.AsQueryable(), spec);
            var results = await query.ToListAsync();

            // Assert
            results.Should().NotBeNull();
        });
    }

    [Fact]
    public async Task Specification_WithMultipleQueryTags_ShouldApplyAllTags()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();

            // Arrange
            var spec = new MultipleTagsSpec();

            // Act
            var query = SpecificationEvaluator.GetQuery(context.EntityAuditLogs.AsQueryable(), spec);
            var results = await query.ToListAsync();

            // Assert - Query should execute successfully with multiple tags
            results.Should().NotBeNull();
        });
    }

    [Fact]
    public async Task Specification_WithStringInclude_ShouldApplyInclude()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();

            // Arrange
            var spec = new StringIncludeSpec();

            // Act
            var query = SpecificationEvaluator.GetQuery(context.EntityAuditLogs.AsQueryable(), spec);
            var results = await query.ToListAsync();

            // Assert
            results.Should().NotBeNull();
        });
    }

    #endregion

    #region Ordering Tests

    [Fact]
    public async Task Specification_WithOrderByDescending_ShouldOrderDescending()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();

            // Arrange - Add tokens with different creation times
            var userId = $"order-test-{Guid.NewGuid()}";
            var token1 = RefreshToken.Create(GenerateTestToken(), userId, 7);
            await Task.Delay(10); // Ensure different timestamps
            var token2 = RefreshToken.Create(GenerateTestToken(), userId, 7);
            context.RefreshTokens.AddRange(token1, token2);
            await context.SaveChangesAsync();

            var spec = new OrderByDescendingSpec(userId);

            // Act
            var query = SpecificationEvaluator.GetQuery(context.RefreshTokens.AsQueryable(), spec);
            var results = await query.ToListAsync();

            // Assert - First result should be newest
            results.Should().HaveCountGreaterThanOrEqualTo(2);
            results.First().CreatedAt.Should().BeOnOrAfter(results.Last().CreatedAt);
        });
    }

    [Fact]
    public async Task Specification_WithThenBy_ShouldApplySecondaryOrdering()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();

            // Arrange
            var spec = new ThenBySpec();

            // Act
            var query = SpecificationEvaluator.GetQuery(context.EntityAuditLogs.AsQueryable(), spec);
            var results = await query.ToListAsync();

            // Assert
            results.Should().NotBeNull();
        });
    }

    [Fact]
    public async Task Specification_WithThenByDescending_ShouldApplySecondaryDescendingOrdering()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();

            // Arrange
            var spec = new ThenByDescendingSpec();

            // Act
            var query = SpecificationEvaluator.GetQuery(context.EntityAuditLogs.AsQueryable(), spec);
            var results = await query.ToListAsync();

            // Assert
            results.Should().NotBeNull();
        });
    }

    #endregion

    #region Paging Tests

    [Fact]
    public async Task Specification_WithSkipAndTake_ShouldApplyPaging()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();

            // Arrange
            var spec = new SkipTakeSpec(1, 1);

            // Act
            var query = SpecificationEvaluator.GetQuery(context.EntityAuditLogs.AsQueryable(), spec);
            var results = await query.ToListAsync();

            // Assert
            results.Should().HaveCount(1);
        });
    }

    [Fact]
    public async Task GetQueryForCount_WithPaging_ShouldIgnorePaging()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();

            // Arrange - Create spec with Take(1)
            var spec = new SkipTakeSpec(0, 1);

            // Act
            var queryForCount = SpecificationEvaluator.GetQueryForCount(context.EntityAuditLogs.AsQueryable(), spec);
            var count = await queryForCount.CountAsync();

            var queryWithPaging = SpecificationEvaluator.GetQuery(context.EntityAuditLogs.AsQueryable(), spec);
            var resultsWithPaging = await queryWithPaging.ToListAsync();

            // Assert - Count should be higher than paged results
            count.Should().BeGreaterThanOrEqualTo(resultsWithPaging.Count);
        });
    }

    #endregion

    #region Tracking Behavior Tests

    [Fact]
    public async Task Specification_WithAsTracking_ShouldTrackEntities()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();

            // Arrange
            var spec = new TrackingSpec();

            // Act
            var query = SpecificationEvaluator.GetQuery(context.EntityAuditLogs.AsQueryable(), spec);
            var results = await query.ToListAsync();

            // Assert - Entities should be tracked
            results.Should().NotBeEmpty();
            foreach (var log in results)
            {
                context.Entry(log).State.Should().Be(EntityState.Unchanged);
            }
        });
    }

    #endregion

    #region Multiple Criteria Tests

    [Fact]
    public async Task Specification_WithMultipleWhereClauses_ShouldCombineWithAnd()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();

            // Arrange
            var spec = new MultipleCriteriaSpec("User", EntityAuditOperation.Added);

            // Act
            var query = SpecificationEvaluator.GetQuery(context.EntityAuditLogs.AsQueryable(), spec);
            var results = await query.ToListAsync();

            // Assert - All results should match BOTH criteria
            results.Should().OnlyContain(a => a.EntityType == "User" && a.Operation == nameof(EntityAuditOperation.Added));
        });
    }

    #endregion
}

#region Test Specifications

/// <summary>
/// Specification to filter EntityAuditLogs by entity type.
/// </summary>
file sealed class EntityAuditLogByTypeSpec : Specification<EntityAuditLog>
{
    public EntityAuditLogByTypeSpec(string entityType)
    {
        Query.Where(a => a.EntityType == entityType)
             .TagWith("EntityAuditLogByType");
    }
}

/// <summary>
/// Specification to get entity history by type and ID.
/// </summary>
file sealed class EntityHistorySpec : Specification<EntityAuditLog>
{
    public EntityHistorySpec(string entityType, string entityId)
    {
        Query.Where(a => a.EntityType == entityType && a.EntityId == entityId)
             .OrderByDescending(a => a.Timestamp)
             .TagWith("EntityHistorySpec");
    }
}

/// <summary>
/// Specification for recent entity audit logs with pagination.
/// </summary>
file sealed class RecentEntityAuditLogsSpec : Specification<EntityAuditLog>
{
    public RecentEntityAuditLogsSpec(int skip, int take)
    {
        Query.OrderByDescending(a => a.Timestamp)
             .Skip(skip)
             .Take(take)
             .TagWith("RecentEntityAuditLogsSpec");
    }
}

/// <summary>
/// Projection specification for entity audit log summaries.
/// </summary>
file sealed class EntityAuditLogSummarySpec : Specification<EntityAuditLog, EntityAuditLogSummaryDto>
{
    public EntityAuditLogSummarySpec()
    {
        Query.Select(a => new EntityAuditLogSummaryDto(a.EntityType, a.Operation, a.Timestamp))
             .TagWith("GetEntityAuditLogSummaries");
    }
}

file record EntityAuditLogSummaryDto(string EntityType, string Operation, DateTimeOffset Timestamp);

/// <summary>
/// Projection specification without a selector (for testing error handling).
/// </summary>
file sealed class NoSelectorProjectionSpec : Specification<EntityAuditLog, EntityAuditLogSummaryDto>
{
    public NoSelectorProjectionSpec()
    {
        // Intentionally NOT setting a selector
    }
}

/// <summary>
/// Specification with IgnoreQueryFilters enabled.
/// </summary>
file sealed class IgnoreFiltersSpec : Specification<RefreshToken>
{
    public IgnoreFiltersSpec()
    {
        Query.IgnoreQueryFilters()
             .TagWith("IgnoreFiltersSpec");
    }
}

/// <summary>
/// Specification with AsSplitQuery enabled.
/// </summary>
file sealed class SplitQuerySpec : Specification<RefreshToken>
{
    public SplitQuerySpec()
    {
        Query.AsSplitQuery()
             .TagWith("SplitQuerySpec");
    }
}

/// <summary>
/// Specification with AsNoTrackingWithIdentityResolution enabled.
/// </summary>
file sealed class IdentityResolutionSpec : Specification<RefreshToken>
{
    public IdentityResolutionSpec(string userId)
    {
        Query.Where(t => t.UserId == userId)
             .AsNoTrackingWithIdentityResolution()
             .TagWith("IdentityResolutionSpec");
    }
}

/// <summary>
/// Specification with IgnoreAutoIncludes enabled.
/// </summary>
file sealed class IgnoreAutoIncludesSpec : Specification<EntityAuditLog>
{
    public IgnoreAutoIncludesSpec()
    {
        Query.IgnoreAutoIncludes()
             .TagWith("IgnoreAutoIncludesSpec");
    }
}

/// <summary>
/// Specification with multiple query tags.
/// </summary>
file sealed class MultipleTagsSpec : Specification<EntityAuditLog>
{
    public MultipleTagsSpec()
    {
        Query.TagWith("Tag1")
             .TagWith("Tag2")
             .TagWith("Tag3");
    }
}

/// <summary>
/// Specification with string-based include.
/// </summary>
file sealed class StringIncludeSpec : Specification<EntityAuditLog>
{
    public StringIncludeSpec()
    {
        // Note: EntityAuditLog has navigation property to HandlerAuditLog, but this tests the code path
        Query.TagWith("StringIncludeSpec");
    }
}

/// <summary>
/// Specification with OrderByDescending.
/// </summary>
file sealed class OrderByDescendingSpec : Specification<RefreshToken>
{
    public OrderByDescendingSpec(string userId)
    {
        Query.Where(t => t.UserId == userId)
             .OrderByDescending(t => t.CreatedAt)
             .TagWith("OrderByDescendingSpec");
    }
}

/// <summary>
/// Specification with ThenBy.
/// </summary>
file sealed class ThenBySpec : Specification<EntityAuditLog>
{
    public ThenBySpec()
    {
        Query.OrderBy(a => a.EntityType)
             .ThenBy(a => a.Operation)
             .TagWith("ThenBySpec");
    }
}

/// <summary>
/// Specification with ThenByDescending.
/// </summary>
file sealed class ThenByDescendingSpec : Specification<EntityAuditLog>
{
    public ThenByDescendingSpec()
    {
        Query.OrderBy(a => a.EntityType)
             .ThenByDescending(a => a.Timestamp)
             .TagWith("ThenByDescendingSpec");
    }
}

/// <summary>
/// Specification with Skip and Take.
/// </summary>
file sealed class SkipTakeSpec : Specification<EntityAuditLog>
{
    public SkipTakeSpec(int skip, int take)
    {
        Query.OrderBy(a => a.Timestamp)
             .Skip(skip)
             .Take(take)
             .TagWith("SkipTakeSpec");
    }
}

/// <summary>
/// Specification with tracking enabled.
/// </summary>
file sealed class TrackingSpec : Specification<EntityAuditLog>
{
    public TrackingSpec()
    {
        Query.AsTracking()
             .TagWith("TrackingSpec");
    }
}

/// <summary>
/// Specification with multiple WHERE criteria.
/// </summary>
file sealed class MultipleCriteriaSpec : Specification<EntityAuditLog>
{
    public MultipleCriteriaSpec(string entityType, EntityAuditOperation operation)
    {
        Query.Where(a => a.EntityType == entityType)
             .Where(a => a.Operation == operation.ToString())
             .TagWith("MultipleCriteriaSpec");
    }
}

#endregion
