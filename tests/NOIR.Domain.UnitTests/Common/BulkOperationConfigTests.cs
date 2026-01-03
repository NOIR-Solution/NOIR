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
        config.BatchSize.Should().Be(2000);
        config.BulkCopyTimeout.Should().BeNull();
        config.SetOutputIdentity.Should().BeFalse();
        config.PreserveInsertOrder.Should().BeTrue();
        config.PropertiesToInclude.Should().BeNull();
        config.PropertiesToExclude.Should().BeNull();
        config.UpdateByProperties.Should().BeNull();
        config.CalculateStats.Should().BeFalse();
        config.Stats.Should().BeNull();
        config.WithHoldlock.Should().BeTrue();
        config.IncludeGraph.Should().BeFalse();
        config.ConfirmSyncWillDeleteMissingRecords.Should().BeFalse();
        config.ConfirmSyncWithEmptyCollection.Should().BeFalse();
    }

    #endregion

    #region Pre-built Configuration Tests

    [Fact]
    public void Default_ShouldReturnNewInstanceWithDefaults()
    {
        // Act
        var config = BulkOperationConfig.Default;

        // Assert
        config.Should().NotBeNull();
        config.BatchSize.Should().Be(2000);
        config.SetOutputIdentity.Should().BeFalse();
    }

    [Fact]
    public void WithOutputIdentity_ShouldHaveCorrectSettings()
    {
        // Act
        var config = BulkOperationConfig.WithOutputIdentity;

        // Assert
        config.SetOutputIdentity.Should().BeTrue();
        config.PreserveInsertOrder.Should().BeTrue();
    }

    [Fact]
    public void LargeBatch_ShouldHaveCorrectSettings()
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
    public void WithBatchSize_ShouldSetBatchSize()
    {
        // Act
        var config = new BulkOperationConfig().WithBatchSize(3000);

        // Assert
        config.BatchSize.Should().Be(3000);
    }

    [Fact]
    public void WithBatchSize_ShouldReturnSameInstance()
    {
        // Arrange
        var config = new BulkOperationConfig();

        // Act
        var result = config.WithBatchSize(3000);

        // Assert
        result.Should().BeSameAs(config);
    }

    [Fact]
    public void WithIdentityOutput_ShouldSetCorrectProperties()
    {
        // Act
        var config = new BulkOperationConfig().WithIdentityOutput();

        // Assert
        config.SetOutputIdentity.Should().BeTrue();
        config.PreserveInsertOrder.Should().BeTrue();
    }

    [Fact]
    public void IncludeProperties_ShouldSetPropertiesToInclude()
    {
        // Act
        var config = new BulkOperationConfig()
            .IncludeProperties("Name", "Email", "Phone");

        // Assert
        config.PropertiesToInclude.Should().BeEquivalentTo(["Name", "Email", "Phone"]);
    }

    [Fact]
    public void ExcludeProperties_ShouldSetPropertiesToExclude()
    {
        // Act
        var config = new BulkOperationConfig()
            .ExcludeProperties("CreatedAt", "CreatedBy");

        // Assert
        config.PropertiesToExclude.Should().BeEquivalentTo(["CreatedAt", "CreatedBy"]);
    }

    [Fact]
    public void UpdateBy_ShouldSetUpdateByProperties()
    {
        // Act
        var config = new BulkOperationConfig()
            .UpdateBy("Sku", "TenantId");

        // Assert
        config.UpdateByProperties.Should().BeEquivalentTo(["Sku", "TenantId"]);
    }

    [Fact]
    public void WithStats_ShouldEnableStats()
    {
        // Act
        var config = new BulkOperationConfig().WithStats();

        // Assert
        config.CalculateStats.Should().BeTrue();
    }

    [Fact]
    public void WithTimeout_ShouldSetTimeout()
    {
        // Act
        var config = new BulkOperationConfig().WithTimeout(180);

        // Assert
        config.BulkCopyTimeout.Should().Be(180);
    }

    [Fact]
    public void WithoutHoldlock_ShouldDisableHoldlock()
    {
        // Act
        var config = new BulkOperationConfig().WithoutHoldlock();

        // Assert
        config.WithHoldlock.Should().BeFalse();
    }

    [Fact]
    public void ConfirmSyncDeletion_ShouldEnableDeleteFlag()
    {
        // Act
        var config = new BulkOperationConfig().ConfirmSyncDeletion();

        // Assert
        config.ConfirmSyncWillDeleteMissingRecords.Should().BeTrue();
        // Note: ConfirmSyncWithEmptyCollection must be set separately for empty collection syncs
        config.ConfirmSyncWithEmptyCollection.Should().BeFalse();
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
        config.ConfirmSyncWillDeleteMissingRecords.Should().BeTrue();
        config.ConfirmSyncWithEmptyCollection.Should().BeTrue();
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
        config.BatchSize.Should().Be(5000);
        config.BulkCopyTimeout.Should().Be(120);
        config.SetOutputIdentity.Should().BeTrue();
        config.PreserveInsertOrder.Should().BeTrue();
        config.UpdateByProperties.Should().BeEquivalentTo(["Sku"]);
        config.PropertiesToExclude.Should().BeEquivalentTo(["CreatedAt"]);
        config.CalculateStats.Should().BeTrue();
        config.WithHoldlock.Should().BeFalse();
        config.ConfirmSyncWillDeleteMissingRecords.Should().BeTrue();
        // ConfirmSyncDeletion only sets ConfirmSyncWillDeleteMissingRecords
        config.ConfirmSyncWithEmptyCollection.Should().BeFalse();
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
        stats.TotalRowsAffected.Should().Be(175);
    }

    [Fact]
    public void DefaultValues_ShouldBeZero()
    {
        // Act
        var stats = new BulkOperationStats();

        // Assert
        stats.RowsInserted.Should().Be(0);
        stats.RowsUpdated.Should().Be(0);
        stats.RowsDeleted.Should().Be(0);
        stats.TotalRowsAffected.Should().Be(0);
        stats.Duration.Should().Be(TimeSpan.Zero);
        stats.BatchesProcessed.Should().Be(0);
    }

    [Fact]
    public void Duration_ShouldBeSettable()
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(5);

        // Act
        var stats = new BulkOperationStats { Duration = duration };

        // Assert
        stats.Duration.Should().Be(duration);
    }
}
