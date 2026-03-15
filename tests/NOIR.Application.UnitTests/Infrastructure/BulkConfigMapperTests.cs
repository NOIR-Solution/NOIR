namespace NOIR.Application.UnitTests.Infrastructure;

using EFCore.BulkExtensions;
using System.Diagnostics;

/// <summary>
/// Unit tests for BulkConfigMapper.
/// Tests the mapping from NOIR's BulkOperationConfig to EFCore.BulkExtensions' BulkConfig.
/// </summary>
public class BulkConfigMapperTests
{
    #region ToBulkConfig Tests

    [Fact]
    public void ToBulkConfig_WithNullConfig_ShouldUseDefaults()
    {
        // Arrange
        var dbContext = CreateMockDbContext();

        // Act
        var result = BulkConfigMapper.ToBulkConfig(null, dbContext);

        // Assert
        result.ShouldNotBeNull();
        result.BatchSize.ShouldBe(2000);  // Default from BulkOperationConfig
        result.SetOutputIdentity.ShouldBe(false);
        result.PreserveInsertOrder.ShouldBe(true);
        result.WithHoldlock.ShouldBe(true);
    }

    [Fact]
    public void ToBulkConfig_WithDefaultConfig_ShouldMapCorrectly()
    {
        // Arrange
        var config = BulkOperationConfig.Default;
        var dbContext = CreateMockDbContext();

        // Act
        var result = BulkConfigMapper.ToBulkConfig(config, dbContext);

        // Assert
        result.ShouldNotBeNull();
        result.BatchSize.ShouldBe(2000);
        result.SetOutputIdentity.ShouldBe(false);
        result.PreserveInsertOrder.ShouldBe(true);
        result.WithHoldlock.ShouldBe(true);
        result.IncludeGraph.ShouldBe(false);
        result.CalculateStats.ShouldBe(false);
    }

    [Fact]
    public void ToBulkConfig_WithCustomBatchSize_ShouldMapCorrectly()
    {
        // Arrange
        var config = new BulkOperationConfig { BatchSize = 5000 };
        var dbContext = CreateMockDbContext();

        // Act
        var result = BulkConfigMapper.ToBulkConfig(config, dbContext);

        // Assert
        result.BatchSize.ShouldBe(5000);
    }

    [Fact]
    public void ToBulkConfig_WithSetOutputIdentity_ShouldMapCorrectly()
    {
        // Arrange
        var config = new BulkOperationConfig { SetOutputIdentity = true };
        var dbContext = CreateMockDbContext();

        // Act
        var result = BulkConfigMapper.ToBulkConfig(config, dbContext);

        // Assert
        result.SetOutputIdentity.ShouldBe(true);
    }

    [Fact]
    public void ToBulkConfig_WithPreserveInsertOrderFalse_ShouldMapCorrectly()
    {
        // Arrange
        var config = new BulkOperationConfig { PreserveInsertOrder = false };
        var dbContext = CreateMockDbContext();

        // Act
        var result = BulkConfigMapper.ToBulkConfig(config, dbContext);

        // Assert
        result.PreserveInsertOrder.ShouldBe(false);
    }

    [Fact]
    public void ToBulkConfig_WithBulkCopyTimeout_ShouldMapCorrectly()
    {
        // Arrange
        var config = new BulkOperationConfig { BulkCopyTimeout = 120 };
        var dbContext = CreateMockDbContext();

        // Act
        var result = BulkConfigMapper.ToBulkConfig(config, dbContext);

        // Assert
        result.BulkCopyTimeout.ShouldBe(120);
    }

    [Fact]
    public void ToBulkConfig_WithNullBulkCopyTimeout_ShouldNotSetTimeout()
    {
        // Arrange
        var config = new BulkOperationConfig { BulkCopyTimeout = null };
        var dbContext = CreateMockDbContext();

        // Act
        var result = BulkConfigMapper.ToBulkConfig(config, dbContext);

        // Assert - When input is null, timeout is not explicitly set
        result.BulkCopyTimeout.ShouldBeNull();
    }

    [Fact]
    public void ToBulkConfig_WithWithHoldlockFalse_ShouldMapCorrectly()
    {
        // Arrange
        var config = new BulkOperationConfig { WithHoldlock = false };
        var dbContext = CreateMockDbContext();

        // Act
        var result = BulkConfigMapper.ToBulkConfig(config, dbContext);

        // Assert
        result.WithHoldlock.ShouldBe(false);
    }

    [Fact]
    public void ToBulkConfig_WithIncludeGraph_ShouldMapCorrectly()
    {
        // Arrange
        var config = new BulkOperationConfig { IncludeGraph = true };
        var dbContext = CreateMockDbContext();

        // Act
        var result = BulkConfigMapper.ToBulkConfig(config, dbContext);

        // Assert
        result.IncludeGraph.ShouldBe(true);
    }

    [Fact]
    public void ToBulkConfig_WithCalculateStats_ShouldMapCorrectly()
    {
        // Arrange
        var config = new BulkOperationConfig { CalculateStats = true };
        var dbContext = CreateMockDbContext();

        // Act
        var result = BulkConfigMapper.ToBulkConfig(config, dbContext);

        // Assert
        result.CalculateStats.ShouldBe(true);
    }

    [Fact]
    public void ToBulkConfig_WithPropertiesToInclude_ShouldMapCorrectly()
    {
        // Arrange
        var config = new BulkOperationConfig
        {
            PropertiesToInclude = new List<string> { "Name", "Email", "Status" }
        };
        var dbContext = CreateMockDbContext();

        // Act
        var result = BulkConfigMapper.ToBulkConfig(config, dbContext);

        // Assert
        result.PropertiesToInclude.ShouldNotBeNull();
        result.PropertiesToInclude.Count().ShouldBe(3);
        result.PropertiesToInclude.ShouldContain("Name");
        result.PropertiesToInclude.ShouldContain("Email");
        result.PropertiesToInclude.ShouldContain("Status");
    }

    [Fact]
    public void ToBulkConfig_WithEmptyPropertiesToInclude_ShouldNotSetProperty()
    {
        // Arrange
        var config = new BulkOperationConfig
        {
            PropertiesToInclude = new List<string>()
        };
        var dbContext = CreateMockDbContext();

        // Act
        var result = BulkConfigMapper.ToBulkConfig(config, dbContext);

        // Assert
        result.PropertiesToInclude.ShouldBeNull();
    }

    [Fact]
    public void ToBulkConfig_WithPropertiesToExclude_ShouldMapCorrectly()
    {
        // Arrange
        var config = new BulkOperationConfig
        {
            PropertiesToExclude = new List<string> { "CreatedAt", "CreatedBy" }
        };
        var dbContext = CreateMockDbContext();

        // Act
        var result = BulkConfigMapper.ToBulkConfig(config, dbContext);

        // Assert
        result.PropertiesToExclude.ShouldNotBeNull();
        result.PropertiesToExclude.Count().ShouldBe(2);
        result.PropertiesToExclude.ShouldContain("CreatedAt");
        result.PropertiesToExclude.ShouldContain("CreatedBy");
    }

    [Fact]
    public void ToBulkConfig_WithEmptyPropertiesToExclude_ShouldNotSetProperty()
    {
        // Arrange
        var config = new BulkOperationConfig
        {
            PropertiesToExclude = new List<string>()
        };
        var dbContext = CreateMockDbContext();

        // Act
        var result = BulkConfigMapper.ToBulkConfig(config, dbContext);

        // Assert
        result.PropertiesToExclude.ShouldBeNull();
    }

    [Fact]
    public void ToBulkConfig_WithUpdateByProperties_ShouldMapCorrectly()
    {
        // Arrange
        var config = new BulkOperationConfig
        {
            UpdateByProperties = new List<string> { "Email", "TenantId" }
        };
        var dbContext = CreateMockDbContext();

        // Act
        var result = BulkConfigMapper.ToBulkConfig(config, dbContext);

        // Assert
        result.UpdateByProperties.ShouldNotBeNull();
        result.UpdateByProperties.Count().ShouldBe(2);
        result.UpdateByProperties.ShouldContain("Email");
        result.UpdateByProperties.ShouldContain("TenantId");
    }

    [Fact]
    public void ToBulkConfig_WithEmptyUpdateByProperties_ShouldNotSetProperty()
    {
        // Arrange
        var config = new BulkOperationConfig
        {
            UpdateByProperties = new List<string>()
        };
        var dbContext = CreateMockDbContext();

        // Act
        var result = BulkConfigMapper.ToBulkConfig(config, dbContext);

        // Assert
        result.UpdateByProperties.ShouldBeNull();
    }

    #endregion

    #region Static Config Tests

    [Fact]
    public void BulkOperationConfig_Default_ShouldHaveCorrectValues()
    {
        // Act
        var config = BulkOperationConfig.Default;

        // Assert
        config.BatchSize.ShouldBe(2000);
        config.SetOutputIdentity.ShouldBe(false);
        config.PreserveInsertOrder.ShouldBe(true);
        config.WithHoldlock.ShouldBe(true);
        config.IncludeGraph.ShouldBe(false);
        config.CalculateStats.ShouldBe(false);
    }

    [Fact]
    public void BulkOperationConfig_WithOutputIdentity_ShouldHaveCorrectValues()
    {
        // Act
        var config = BulkOperationConfig.WithOutputIdentity;

        // Assert
        config.SetOutputIdentity.ShouldBe(true);
        config.PreserveInsertOrder.ShouldBe(true);
    }

    [Fact]
    public void BulkOperationConfig_LargeBatch_ShouldHaveCorrectValues()
    {
        // Act
        var config = BulkOperationConfig.LargeBatch;

        // Assert
        config.BatchSize.ShouldBe(5000);
        config.BulkCopyTimeout.ShouldBe(120);
    }

    #endregion

    #region Fluent API Tests

    [Fact]
    public void BulkOperationConfig_WithBatchSize_ShouldSetValue()
    {
        // Arrange & Act
        var config = new BulkOperationConfig().WithBatchSize(3000);

        // Assert
        config.BatchSize.ShouldBe(3000);
    }

    [Fact]
    public void BulkOperationConfig_WithBatchSize_ZeroOrNegative_ShouldThrow()
    {
        // Arrange
        var config = new BulkOperationConfig();

        // Act & Assert
        var act1 = () => config.WithBatchSize(0);
        var act2 = () => config.WithBatchSize(-1);

        Should.Throw<ArgumentOutOfRangeException>(act1);
        Should.Throw<ArgumentOutOfRangeException>(act2);
    }

    [Fact]
    public void BulkOperationConfig_WithIdentityOutput_ShouldSetValues()
    {
        // Arrange & Act
        var config = new BulkOperationConfig().WithIdentityOutput();

        // Assert
        config.SetOutputIdentity.ShouldBe(true);
        config.PreserveInsertOrder.ShouldBe(true);
    }

    [Fact]
    public void BulkOperationConfig_IncludeProperties_ShouldSetValues()
    {
        // Arrange & Act
        var config = new BulkOperationConfig().IncludeProperties("Name", "Email");

        // Assert
        config.PropertiesToInclude.Count().ShouldBe(2);
        config.PropertiesToInclude.ShouldContain("Name");
        config.PropertiesToInclude.ShouldContain("Email");
    }

    [Fact]
    public void BulkOperationConfig_ExcludeProperties_ShouldSetValues()
    {
        // Arrange & Act
        var config = new BulkOperationConfig().ExcludeProperties("CreatedAt", "CreatedBy");

        // Assert
        config.PropertiesToExclude.Count().ShouldBe(2);
        config.PropertiesToExclude.ShouldContain("CreatedAt");
        config.PropertiesToExclude.ShouldContain("CreatedBy");
    }

    [Fact]
    public void BulkOperationConfig_UpdateBy_ShouldSetValues()
    {
        // Arrange & Act
        var config = new BulkOperationConfig().UpdateBy("Email", "TenantId");

        // Assert
        config.UpdateByProperties.Count().ShouldBe(2);
        config.UpdateByProperties.ShouldContain("Email");
        config.UpdateByProperties.ShouldContain("TenantId");
    }

    [Fact]
    public void BulkOperationConfig_WithStats_ShouldSetValue()
    {
        // Arrange & Act
        var config = new BulkOperationConfig().WithStats();

        // Assert
        config.CalculateStats.ShouldBe(true);
    }

    [Fact]
    public void BulkOperationConfig_WithTimeout_ShouldSetValue()
    {
        // Arrange & Act
        var config = new BulkOperationConfig().WithTimeout(60);

        // Assert
        config.BulkCopyTimeout.ShouldBe(60);
    }

    [Fact]
    public void BulkOperationConfig_WithoutHoldlock_ShouldSetValue()
    {
        // Arrange & Act
        var config = new BulkOperationConfig().WithoutHoldlock();

        // Assert
        config.WithHoldlock.ShouldBe(false);
    }

    [Fact]
    public void BulkOperationConfig_ConfirmSyncDeletion_ShouldSetValue()
    {
        // Arrange & Act
        var config = new BulkOperationConfig().ConfirmSyncDeletion();

        // Assert
        config.ConfirmSyncWillDeleteMissingRecords.ShouldBe(true);
    }

    [Fact]
    public void BulkOperationConfig_FluentChaining_ShouldWork()
    {
        // Arrange & Act
        var config = new BulkOperationConfig()
            .WithBatchSize(3000)
            .WithIdentityOutput()
            .WithStats()
            .WithTimeout(120)
            .WithoutHoldlock()
            .ExcludeProperties("CreatedAt");

        // Assert
        config.BatchSize.ShouldBe(3000);
        config.SetOutputIdentity.ShouldBe(true);
        config.PreserveInsertOrder.ShouldBe(true);
        config.CalculateStats.ShouldBe(true);
        config.BulkCopyTimeout.ShouldBe(120);
        config.WithHoldlock.ShouldBe(false);
        config.PropertiesToExclude.ShouldContain("CreatedAt");
    }

    #endregion

    #region UpdateStats Tests

    [Fact]
    public void UpdateStats_WhenConfigIsNull_ShouldNotThrow()
    {
        // Arrange
        var bulkConfig = new BulkConfig();
        var stopwatch = new Stopwatch();

        // Act
        var act = () => BulkConfigMapper.UpdateStats(null, bulkConfig, stopwatch);

        // Assert
        act.ShouldNotThrow();
    }

    [Fact]
    public void UpdateStats_WhenCalculateStatsFalse_ShouldNotUpdateStats()
    {
        // Arrange
        var config = new BulkOperationConfig { CalculateStats = false };
        var bulkConfig = new BulkConfig();
        var stopwatch = new Stopwatch();

        // Act
        BulkConfigMapper.UpdateStats(config, bulkConfig, stopwatch);

        // Assert
        config.Stats.ShouldBeNull();
    }

    [Fact]
    public void UpdateStats_WhenCalculateStatsTrue_ShouldCreateStats()
    {
        // Arrange
        var config = new BulkOperationConfig { CalculateStats = true };
        var bulkConfig = new BulkConfig();
        // Note: StatsInfo is read-only and populated by EFCore.BulkExtensions during actual operations
        // For unit testing, we test with null StatsInfo (default values)
        var stopwatch = Stopwatch.StartNew();
        stopwatch.Stop();

        // Act
        BulkConfigMapper.UpdateStats(config, bulkConfig, stopwatch, entityCount: 100);

        // Assert
        config.Stats.ShouldNotBeNull();
        // With null StatsInfo, all counts should be 0
        config.Stats!.RowsInserted.ShouldBe(0);
        config.Stats.RowsUpdated.ShouldBe(0);
        config.Stats.RowsDeleted.ShouldBe(0);
    }

    [Fact]
    public void UpdateStats_WithEntityCount_ShouldCalculateBatches()
    {
        // Arrange
        var config = new BulkOperationConfig
        {
            CalculateStats = true,
            BatchSize = 100
        };
        var bulkConfig = new BulkConfig();
        var stopwatch = Stopwatch.StartNew();
        stopwatch.Stop();

        // Act
        BulkConfigMapper.UpdateStats(config, bulkConfig, stopwatch, entityCount: 250);

        // Assert
        config.Stats.ShouldNotBeNull();
        config.Stats!.BatchesProcessed.ShouldBe(3);  // 250 / 100 = 2.5, ceil = 3
    }

    [Fact]
    public void UpdateStats_WithZeroEntityCount_ShouldHaveZeroBatches()
    {
        // Arrange
        var config = new BulkOperationConfig { CalculateStats = true };
        var bulkConfig = new BulkConfig();
        var stopwatch = Stopwatch.StartNew();
        stopwatch.Stop();

        // Act
        BulkConfigMapper.UpdateStats(config, bulkConfig, stopwatch, entityCount: 0);

        // Assert
        config.Stats.ShouldNotBeNull();
        config.Stats!.BatchesProcessed.ShouldBe(0);
    }

    [Fact]
    public void UpdateStats_ShouldCaptureDuration()
    {
        // Arrange
        var config = new BulkOperationConfig { CalculateStats = true };
        var bulkConfig = new BulkConfig();
        var stopwatch = Stopwatch.StartNew();
        Thread.Sleep(10);  // Small delay to ensure measurable duration
        stopwatch.Stop();

        // Act
        BulkConfigMapper.UpdateStats(config, bulkConfig, stopwatch);

        // Assert
        config.Stats.ShouldNotBeNull();
        config.Stats!.Duration.ShouldBeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public void UpdateStats_WithDefaultBulkConfig_ShouldUseZeroValues()
    {
        // Arrange
        var config = new BulkOperationConfig { CalculateStats = true };
        var bulkConfig = new BulkConfig();  // StatsInfo is read-only, defaults to null
        var stopwatch = new Stopwatch();

        // Act
        BulkConfigMapper.UpdateStats(config, bulkConfig, stopwatch);

        // Assert
        config.Stats.ShouldNotBeNull();
        config.Stats!.RowsInserted.ShouldBe(0);
        config.Stats.RowsUpdated.ShouldBe(0);
        config.Stats.RowsDeleted.ShouldBe(0);
    }

    #endregion

    #region BulkOperationStats Tests

    [Fact]
    public void BulkOperationStats_TotalRowsAffected_ShouldCalculateSum()
    {
        // Arrange
        var stats = new BulkOperationStats
        {
            RowsInserted = 10,
            RowsUpdated = 5,
            RowsDeleted = 3
        };

        // Assert
        stats.TotalRowsAffected.ShouldBe(18);
    }

    [Fact]
    public void BulkOperationStats_ToString_ShouldFormatCorrectly()
    {
        // Arrange
        var stats = new BulkOperationStats
        {
            RowsInserted = 10,
            RowsUpdated = 5,
            RowsDeleted = 3,
            Duration = TimeSpan.FromMilliseconds(150),
            BatchesProcessed = 2
        };

        // Act
        var result = stats.ToString();

        // Assert
        result.ShouldContain("18 rows affected");
        result.ShouldContain("Inserted: 10");
        result.ShouldContain("Updated: 5");
        result.ShouldContain("Deleted: 3");
        result.ShouldContain("150ms");
        result.ShouldContain("2 batches");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a mock DbContext for testing.
    /// BulkConfigMapper only needs database connection for transaction handling.
    /// </summary>
    private static DbContext CreateMockDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Create a minimal mock since we only need the Database property
        var mock = new Mock<DbContext>();
        var dbMock = new Mock<Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade>(mock.Object);
        dbMock.Setup(x => x.CurrentTransaction).Returns((Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction?)null);
        mock.Setup(x => x.Database).Returns(dbMock.Object);

        return mock.Object;
    }

    #endregion
}
