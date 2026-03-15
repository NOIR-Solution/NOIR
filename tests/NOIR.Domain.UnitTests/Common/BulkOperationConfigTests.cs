namespace NOIR.Domain.UnitTests.Common;

/// <summary>
/// Unit tests for BulkOperationConfig and BulkOperationStats.
/// Tests default values, fluent API, and pre-built configurations.
/// </summary>
public class BulkOperationConfigTests
{
    #region Default Values Tests

    [Fact]
    public void DefaultConfig_ShouldHaveCorrectDefaults()
    {
        // Act
        var config = new BulkOperationConfig();

        // Assert
        config.BatchSize.ShouldBe(2000);
        config.BulkCopyTimeout.ShouldBeNull();
        config.SetOutputIdentity.ShouldBeFalse();
        config.PreserveInsertOrder.ShouldBeTrue();
        config.PropertiesToInclude.ShouldBeNull();
        config.PropertiesToExclude.ShouldBeNull();
        config.UpdateByProperties.ShouldBeNull();
        config.CalculateStats.ShouldBeFalse();
        config.Stats.ShouldBeNull();
        config.WithHoldlock.ShouldBeTrue();
        config.IncludeGraph.ShouldBeFalse();
        config.ConfirmSyncWillDeleteMissingRecords.ShouldBeFalse();
        config.ConfirmSyncWithEmptyCollection.ShouldBeFalse();
    }

    #endregion

    #region Pre-built Configuration Tests

    [Fact]
    public void Default_ShouldReturnNewInstanceWithDefaults()
    {
        // Act
        var config = BulkOperationConfig.Default;

        // Assert
        config.ShouldNotBeNull();
        config.BatchSize.ShouldBe(2000);
        config.SetOutputIdentity.ShouldBeFalse();
    }

    [Fact]
    public void WithOutputIdentity_ShouldHaveCorrectSettings()
    {
        // Act
        var config = BulkOperationConfig.WithOutputIdentity;

        // Assert
        config.SetOutputIdentity.ShouldBeTrue();
        config.PreserveInsertOrder.ShouldBeTrue();
    }

    [Fact]
    public void LargeBatch_ShouldHaveCorrectSettings()
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
    public void WithBatchSize_ShouldSetBatchSize()
    {
        // Act
        var config = new BulkOperationConfig().WithBatchSize(3000);

        // Assert
        config.BatchSize.ShouldBe(3000);
    }

    [Fact]
    public void WithBatchSize_ShouldReturnSameInstance()
    {
        // Arrange
        var config = new BulkOperationConfig();

        // Act
        var result = config.WithBatchSize(3000);

        // Assert
        result.ShouldBeSameAs(config);
    }

    [Fact]
    public void WithIdentityOutput_ShouldSetCorrectProperties()
    {
        // Act
        var config = new BulkOperationConfig().WithIdentityOutput();

        // Assert
        config.SetOutputIdentity.ShouldBeTrue();
        config.PreserveInsertOrder.ShouldBeTrue();
    }

    [Fact]
    public void IncludeProperties_ShouldSetPropertiesToInclude()
    {
        // Act
        var config = new BulkOperationConfig()
            .IncludeProperties("Name", "Email", "Phone");

        // Assert
        config.PropertiesToInclude.ShouldBe(["Name", "Email", "Phone"]);
    }

    [Fact]
    public void ExcludeProperties_ShouldSetPropertiesToExclude()
    {
        // Act
        var config = new BulkOperationConfig()
            .ExcludeProperties("CreatedAt", "CreatedBy");

        // Assert
        config.PropertiesToExclude.ShouldBe(["CreatedAt", "CreatedBy"]);
    }

    [Fact]
    public void UpdateBy_ShouldSetUpdateByProperties()
    {
        // Act
        var config = new BulkOperationConfig()
            .UpdateBy("Sku", "TenantId");

        // Assert
        config.UpdateByProperties.ShouldBe(["Sku", "TenantId"]);
    }

    [Fact]
    public void WithStats_ShouldEnableStats()
    {
        // Act
        var config = new BulkOperationConfig().WithStats();

        // Assert
        config.CalculateStats.ShouldBeTrue();
    }

    [Fact]
    public void WithTimeout_ShouldSetTimeout()
    {
        // Act
        var config = new BulkOperationConfig().WithTimeout(180);

        // Assert
        config.BulkCopyTimeout.ShouldBe(180);
    }

    [Fact]
    public void WithoutHoldlock_ShouldDisableHoldlock()
    {
        // Act
        var config = new BulkOperationConfig().WithoutHoldlock();

        // Assert
        config.WithHoldlock.ShouldBeFalse();
    }

    [Fact]
    public void ConfirmSyncDeletion_ShouldEnableDeleteFlag()
    {
        // Act
        var config = new BulkOperationConfig().ConfirmSyncDeletion();

        // Assert
        config.ConfirmSyncWillDeleteMissingRecords.ShouldBeTrue();
        // Note: ConfirmSyncWithEmptyCollection must be set separately for empty collection syncs
        config.ConfirmSyncWithEmptyCollection.ShouldBeFalse();
    }

    [Fact]
    public void ConfirmSyncDeletion_WithEmptyConfirmation_ShouldEnableBothFlags()
    {
        // Act - Manually set both flags for complete sync confirmation
        var config = new BulkOperationConfig
        {
            ConfirmSyncWillDeleteMissingRecords = true,
            ConfirmSyncWithEmptyCollection = true
        };

        // Assert
        config.ConfirmSyncWillDeleteMissingRecords.ShouldBeTrue();
        config.ConfirmSyncWithEmptyCollection.ShouldBeTrue();
    }

    [Fact]
    public void FluentAPI_ShouldSupportChaining()
    {
        // Act
        var config = new BulkOperationConfig()
            .WithBatchSize(5000)
            .WithTimeout(120)
            .WithIdentityOutput()
            .UpdateBy("Sku")
            .ExcludeProperties("CreatedAt")
            .WithStats()
            .WithoutHoldlock()
            .ConfirmSyncDeletion();

        // Assert
        config.BatchSize.ShouldBe(5000);
        config.BulkCopyTimeout.ShouldBe(120);
        config.SetOutputIdentity.ShouldBeTrue();
        config.PreserveInsertOrder.ShouldBeTrue();
        config.UpdateByProperties.ShouldBe(["Sku"]);
        config.PropertiesToExclude.ShouldBe(["CreatedAt"]);
        config.CalculateStats.ShouldBeTrue();
        config.WithHoldlock.ShouldBeFalse();
        config.ConfirmSyncWillDeleteMissingRecords.ShouldBeTrue();
        // ConfirmSyncDeletion only sets ConfirmSyncWillDeleteMissingRecords
        config.ConfirmSyncWithEmptyCollection.ShouldBeFalse();
    }

    #endregion
}

/// <summary>
/// Unit tests for BulkOperationStats.
/// </summary>
public class BulkOperationStatsTests
{
    [Fact]
    public void TotalRowsAffected_ShouldSumAllOperations()
    {
        // Arrange
        var stats = new BulkOperationStats
        {
            RowsInserted = 100,
            RowsUpdated = 50,
            RowsDeleted = 25
        };

        // Act & Assert
        stats.TotalRowsAffected.ShouldBe(175);
    }

    [Fact]
    public void DefaultValues_ShouldBeZero()
    {
        // Act
        var stats = new BulkOperationStats();

        // Assert
        stats.RowsInserted.ShouldBe(0);
        stats.RowsUpdated.ShouldBe(0);
        stats.RowsDeleted.ShouldBe(0);
        stats.TotalRowsAffected.ShouldBe(0);
        stats.Duration.ShouldBe(TimeSpan.Zero);
        stats.BatchesProcessed.ShouldBe(0);
    }

    [Fact]
    public void Duration_ShouldBeSettable()
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(5);

        // Act
        var stats = new BulkOperationStats { Duration = duration };

        // Assert
        stats.Duration.ShouldBe(duration);
    }
}
