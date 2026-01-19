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
        result.Should().NotBeNull();
        result.BatchSize.Should().Be(2000);  // Default from BulkOperationConfig
        result.SetOutputIdentity.Should().BeFalse();
        result.PreserveInsertOrder.Should().BeTrue();
        result.WithHoldlock.Should().BeTrue();
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
        result.Should().NotBeNull();
        result.BatchSize.Should().Be(2000);
        result.SetOutputIdentity.Should().BeFalse();
        result.PreserveInsertOrder.Should().BeTrue();
        result.WithHoldlock.Should().BeTrue();
        result.IncludeGraph.Should().BeFalse();
        result.CalculateStats.Should().BeFalse();
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
        result.BatchSize.Should().Be(5000);
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
        result.SetOutputIdentity.Should().BeTrue();
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
        result.PreserveInsertOrder.Should().BeFalse();
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
        result.BulkCopyTimeout.Should().Be(120);
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
        result.BulkCopyTimeout.Should().BeNull();
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
        result.WithHoldlock.Should().BeFalse();
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
        result.IncludeGraph.Should().BeTrue();
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
        result.CalculateStats.Should().BeTrue();
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
        result.PropertiesToInclude.Should().NotBeNull();
        result.PropertiesToInclude.Should().HaveCount(3);
        result.PropertiesToInclude.Should().Contain("Name");
        result.PropertiesToInclude.Should().Contain("Email");
        result.PropertiesToInclude.Should().Contain("Status");
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
        result.PropertiesToInclude.Should().BeNull();
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
        result.PropertiesToExclude.Should().NotBeNull();
        result.PropertiesToExclude.Should().HaveCount(2);
        result.PropertiesToExclude.Should().Contain("CreatedAt");
        result.PropertiesToExclude.Should().Contain("CreatedBy");
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
        result.PropertiesToExclude.Should().BeNull();
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
        result.UpdateByProperties.Should().NotBeNull();
        result.UpdateByProperties.Should().HaveCount(2);
        result.UpdateByProperties.Should().Contain("Email");
        result.UpdateByProperties.Should().Contain("TenantId");
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
        result.UpdateByProperties.Should().BeNull();
    }

    #endregion

    #region Static Config Tests

    [Fact]
    public void BulkOperationConfig_Default_ShouldHaveCorrectValues()
    {
        // Act
        var config = BulkOperationConfig.Default;

        // Assert
        config.BatchSize.Should().Be(2000);
        config.SetOutputIdentity.Should().BeFalse();
        config.PreserveInsertOrder.Should().BeTrue();
        config.WithHoldlock.Should().BeTrue();
        config.IncludeGraph.Should().BeFalse();
        config.CalculateStats.Should().BeFalse();
    }

    [Fact]
    public void BulkOperationConfig_WithOutputIdentity_ShouldHaveCorrectValues()
    {
        // Act
        var config = BulkOperationConfig.WithOutputIdentity;

        // Assert
        config.SetOutputIdentity.Should().BeTrue();
        config.PreserveInsertOrder.Should().BeTrue();
    }

    [Fact]
    public void BulkOperationConfig_LargeBatch_ShouldHaveCorrectValues()
    {
        // Act
        var config = BulkOperationConfig.LargeBatch;

        // Assert
        config.BatchSize.Should().Be(5000);
        config.BulkCopyTimeout.Should().Be(120);
    }

    #endregion

    #region Fluent API Tests

    [Fact]
    public void BulkOperationConfig_WithBatchSize_ShouldSetValue()
    {
        // Arrange & Act
        var config = new BulkOperationConfig().WithBatchSize(3000);

        // Assert
        config.BatchSize.Should().Be(3000);
    }

    [Fact]
    public void BulkOperationConfig_WithBatchSize_ZeroOrNegative_ShouldThrow()
    {
        // Arrange
        var config = new BulkOperationConfig();

        // Act & Assert
        var act1 = () => config.WithBatchSize(0);
        var act2 = () => config.WithBatchSize(-1);

        act1.Should().Throw<ArgumentOutOfRangeException>();
        act2.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void BulkOperationConfig_WithIdentityOutput_ShouldSetValues()
    {
        // Arrange & Act
        var config = new BulkOperationConfig().WithIdentityOutput();

        // Assert
        config.SetOutputIdentity.Should().BeTrue();
        config.PreserveInsertOrder.Should().BeTrue();
    }

    [Fact]
    public void BulkOperationConfig_IncludeProperties_ShouldSetValues()
    {
        // Arrange & Act
        var config = new BulkOperationConfig().IncludeProperties("Name", "Email");

        // Assert
        config.PropertiesToInclude.Should().HaveCount(2);
        config.PropertiesToInclude.Should().Contain("Name");
        config.PropertiesToInclude.Should().Contain("Email");
    }

    [Fact]
    public void BulkOperationConfig_ExcludeProperties_ShouldSetValues()
    {
        // Arrange & Act
        var config = new BulkOperationConfig().ExcludeProperties("CreatedAt", "CreatedBy");

        // Assert
        config.PropertiesToExclude.Should().HaveCount(2);
        config.PropertiesToExclude.Should().Contain("CreatedAt");
        config.PropertiesToExclude.Should().Contain("CreatedBy");
    }

    [Fact]
    public void BulkOperationConfig_UpdateBy_ShouldSetValues()
    {
        // Arrange & Act
        var config = new BulkOperationConfig().UpdateBy("Email", "TenantId");

        // Assert
        config.UpdateByProperties.Should().HaveCount(2);
        config.UpdateByProperties.Should().Contain("Email");
        config.UpdateByProperties.Should().Contain("TenantId");
    }

    [Fact]
    public void BulkOperationConfig_WithStats_ShouldSetValue()
    {
        // Arrange & Act
        var config = new BulkOperationConfig().WithStats();

        // Assert
        config.CalculateStats.Should().BeTrue();
    }

    [Fact]
    public void BulkOperationConfig_WithTimeout_ShouldSetValue()
    {
        // Arrange & Act
        var config = new BulkOperationConfig().WithTimeout(60);

        // Assert
        config.BulkCopyTimeout.Should().Be(60);
    }

    [Fact]
    public void BulkOperationConfig_WithoutHoldlock_ShouldSetValue()
    {
        // Arrange & Act
        var config = new BulkOperationConfig().WithoutHoldlock();

        // Assert
        config.WithHoldlock.Should().BeFalse();
    }

    [Fact]
    public void BulkOperationConfig_ConfirmSyncDeletion_ShouldSetValue()
    {
        // Arrange & Act
        var config = new BulkOperationConfig().ConfirmSyncDeletion();

        // Assert
        config.ConfirmSyncWillDeleteMissingRecords.Should().BeTrue();
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
        config.BatchSize.Should().Be(3000);
        config.SetOutputIdentity.Should().BeTrue();
        config.PreserveInsertOrder.Should().BeTrue();
        config.CalculateStats.Should().BeTrue();
        config.BulkCopyTimeout.Should().Be(120);
        config.WithHoldlock.Should().BeFalse();
        config.PropertiesToExclude.Should().Contain("CreatedAt");
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
        act.Should().NotThrow();
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
        config.Stats.Should().BeNull();
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
        config.Stats.Should().NotBeNull();
        // With null StatsInfo, all counts should be 0
        config.Stats!.RowsInserted.Should().Be(0);
        config.Stats.RowsUpdated.Should().Be(0);
        config.Stats.RowsDeleted.Should().Be(0);
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
        config.Stats.Should().NotBeNull();
        config.Stats!.BatchesProcessed.Should().Be(3);  // 250 / 100 = 2.5, ceil = 3
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
        config.Stats.Should().NotBeNull();
        config.Stats!.BatchesProcessed.Should().Be(0);
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
        config.Stats.Should().NotBeNull();
        config.Stats!.Duration.Should().BeGreaterThan(TimeSpan.Zero);
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
        config.Stats.Should().NotBeNull();
        config.Stats!.RowsInserted.Should().Be(0);
        config.Stats.RowsUpdated.Should().Be(0);
        config.Stats.RowsDeleted.Should().Be(0);
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
        stats.TotalRowsAffected.Should().Be(18);
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
        result.Should().Contain("18 rows affected");
        result.Should().Contain("Inserted: 10");
        result.Should().Contain("Updated: 5");
        result.Should().Contain("Deleted: 3");
        result.Should().Contain("150ms");
        result.Should().Contain("2 batches");
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
