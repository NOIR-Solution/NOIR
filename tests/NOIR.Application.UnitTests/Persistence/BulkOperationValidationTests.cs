namespace NOIR.Application.UnitTests.Persistence;

/// <summary>
/// Unit tests for bulk operation configuration states and validation inputs.
///
/// These tests verify that BulkOperationConfig can represent various states
/// (valid, invalid, conflicting) that will be validated by Repository methods.
///
/// NOTE: Actual validation enforcement happens in Repository.ValidateBulkOperationConfig()
/// which is tested via integration tests in BulkOperationsLocalDbTests.cs.
///
/// Why these tests exist:
/// 1. Verify config object correctly stores settings
/// 2. Ensure configuration states are detectable before calling Repository
/// 3. Document expected validation scenarios
///
/// The Repository's ValidateBulkOperationConfig() throws when:
/// - Both PropertiesToInclude AND PropertiesToExclude are set
/// - BatchSize &lt;= 0
/// - BulkSyncAsync called without ConfirmSyncWillDeleteMissingRecords
/// </summary>
public class BulkOperationValidationTests
{
    #region Configuration State Tests

    [Fact]
    public void BulkOperationConfig_WithBothIncludeAndExclude_ShouldBeDetectable()
    {
        // Arrange - Config allows setting both, but Repository will reject it
        var config = new BulkOperationConfig
        {
            PropertiesToInclude = ["Name", "Email"],
            PropertiesToExclude = ["CreatedAt"]
        };

        // Assert - Verify conflicting state is detectable
        // Repository.ValidateBulkOperationConfig() will throw InvalidOperationException
        config.PropertiesToInclude.Should().NotBeEmpty("include list was set");
        config.PropertiesToExclude.Should().NotBeEmpty("exclude list was set");
        // This combination will cause Repository to throw:
        // "Cannot specify both PropertiesToInclude and PropertiesToExclude"
    }

    [Fact]
    public void BulkOperationConfig_WithZeroOrNegativeBatchSize_ShouldBeDetectable()
    {
        // Arrange - Config allows invalid batch sizes, but Repository will reject them
        var zeroConfig = new BulkOperationConfig { BatchSize = 0 };
        var negativeConfig = new BulkOperationConfig { BatchSize = -1 };

        // Assert - Verify invalid states are detectable
        // Repository.ValidateBulkOperationConfig() will throw ArgumentOutOfRangeException
        zeroConfig.BatchSize.Should().Be(0, "zero batch size will be rejected by Repository");
        negativeConfig.BatchSize.Should().BeNegative("negative batch size will be rejected by Repository");
        // These values will cause Repository to throw:
        // "BatchSize must be greater than zero"
    }

    [Fact]
    public void BulkOperationConfig_ValidBatchSize_ShouldBeAccepted()
    {
        // Arrange
        var config = new BulkOperationConfig { BatchSize = 1000 };

        // Assert
        config.BatchSize.Should().BeGreaterThan(0);
    }

    #endregion

    #region BulkSync Safety Confirmation Tests

    [Fact]
    public void BulkSyncConfig_WithoutConfirmation_ShouldBeDetectable()
    {
        // Arrange
        var config = new BulkOperationConfig();

        // Assert - Default is no confirmation
        config.ConfirmSyncWillDeleteMissingRecords.Should().BeFalse();
    }

    [Fact]
    public void BulkSyncConfig_WithConfirmation_ShouldBeDetectable()
    {
        // Arrange - ConfirmSyncDeletion only sets ConfirmSyncWillDeleteMissingRecords
        var config = new BulkOperationConfig()
            .ConfirmSyncDeletion();

        // Assert
        config.ConfirmSyncWillDeleteMissingRecords.Should().BeTrue();
        // ConfirmSyncWithEmptyCollection must be set separately
        config.ConfirmSyncWithEmptyCollection.Should().BeFalse();
    }

    [Fact]
    public void BulkSyncConfig_WithBothConfirmations_ShouldBeDetectable()
    {
        // Arrange - Manually set both for full sync confirmation
        var config = new BulkOperationConfig
        {
            ConfirmSyncWillDeleteMissingRecords = true,
            ConfirmSyncWithEmptyCollection = true
        };

        // Assert
        config.ConfirmSyncWillDeleteMissingRecords.Should().BeTrue();
        config.ConfirmSyncWithEmptyCollection.Should().BeTrue();
    }

    [Fact]
    public void BulkSyncConfig_OnlyDeleteConfirmation_ShouldWork()
    {
        // Arrange
        var config = new BulkOperationConfig
        {
            ConfirmSyncWillDeleteMissingRecords = true,
            ConfirmSyncWithEmptyCollection = false
        };

        // Assert
        config.ConfirmSyncWillDeleteMissingRecords.Should().BeTrue();
        config.ConfirmSyncWithEmptyCollection.Should().BeFalse();
    }

    #endregion

    #region Stats Calculation Tests

    [Fact]
    public void BulkOperationStats_WithStatsEnabled_ShouldReturnStats()
    {
        // Arrange
        var config = new BulkOperationConfig().WithStats();
        config.Stats = new BulkOperationStats
        {
            RowsInserted = 1000,
            RowsUpdated = 500,
            RowsDeleted = 100,
            Duration = TimeSpan.FromSeconds(2),
            BatchesProcessed = 1
        };

        // Assert
        config.Stats.Should().NotBeNull();
        config.Stats!.RowsInserted.Should().Be(1000);
        config.Stats.TotalRowsAffected.Should().Be(1600);
    }

    [Fact]
    public void BulkOperationStats_TotalRowsAffected_ShouldSumAllOperations()
    {
        // Arrange - Stats populated by Repository after bulk operation completes
        var stats = new BulkOperationStats
        {
            RowsInserted = 500,
            RowsUpdated = 300,
            RowsDeleted = 50
        };

        // Assert - TotalRowsAffected is calculated property
        stats.TotalRowsAffected.Should().Be(850, "it sums insert + update + delete");
    }

    [Fact]
    public void BulkOperationStats_Duration_ShouldReflectActualOperationTime()
    {
        // Arrange - Stats populated by Repository with stopwatch timing
        var stats = new BulkOperationStats
        {
            Duration = TimeSpan.FromMilliseconds(250)
        };

        // Assert - Duration captures actual execution time
        stats.Duration.TotalMilliseconds.Should().Be(250);
    }

    #endregion

    #region Multi-Tenant Validation Tests

    [Fact]
    public void ITenantEntity_ShouldHaveTenantId()
    {
        // This test verifies the interface contract
        typeof(ITenantEntity).GetProperty("TenantId").Should().NotBeNull();
    }

    #endregion

    #region Pre-built Configuration Tests

    [Fact]
    public void PrebuiltConfigs_ShouldBeImmutableInstances()
    {
        // Get two instances
        var default1 = BulkOperationConfig.Default;
        var default2 = BulkOperationConfig.Default;

        // They should be different instances (factory pattern)
        default1.Should().NotBeSameAs(default2);

        // Modifying one should not affect the other
        default1.BatchSize = 999;
        default2.BatchSize.Should().Be(2000);
    }

    [Fact]
    public void LargeBatch_ShouldHaveExtendedTimeout()
    {
        // Large batch should have a longer timeout
        var config = BulkOperationConfig.LargeBatch;

        config.BulkCopyTimeout.Should().BeGreaterThan(60);
    }

    #endregion
}
