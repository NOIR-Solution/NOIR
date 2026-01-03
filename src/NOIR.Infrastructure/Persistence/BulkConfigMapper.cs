namespace NOIR.Infrastructure.Persistence;

/// <summary>
/// Maps NOIR's BulkOperationConfig to EFCore.BulkExtensions' BulkConfig.
/// Provides abstraction from the third-party library.
/// </summary>
internal static class BulkConfigMapper
{
    /// <summary>
    /// Creates a BulkConfig from the NOIR configuration.
    /// </summary>
    public static BulkConfig ToBulkConfig(BulkOperationConfig? config, DbContext dbContext)
    {
        config ??= BulkOperationConfig.Default;

        var bulkConfig = new BulkConfig
        {
            BatchSize = config.BatchSize,
            SetOutputIdentity = config.SetOutputIdentity,
            PreserveInsertOrder = config.PreserveInsertOrder,
            WithHoldlock = config.WithHoldlock,
            IncludeGraph = config.IncludeGraph,
            CalculateStats = config.CalculateStats
        };

        if (config.BulkCopyTimeout.HasValue)
        {
            bulkConfig.BulkCopyTimeout = config.BulkCopyTimeout.Value;
        }

        if (config.PropertiesToInclude?.Count > 0)
        {
            bulkConfig.PropertiesToInclude = config.PropertiesToInclude.ToList();
        }

        if (config.PropertiesToExclude?.Count > 0)
        {
            bulkConfig.PropertiesToExclude = config.PropertiesToExclude.ToList();
        }

        if (config.UpdateByProperties?.Count > 0)
        {
            bulkConfig.UpdateByProperties = config.UpdateByProperties.ToList();
        }

        // Use existing transaction if one is active
        var currentTransaction = dbContext.Database.CurrentTransaction;
        if (currentTransaction != null)
        {
            var connection = dbContext.Database.GetDbConnection();
            var transaction = currentTransaction.GetDbTransaction();
            bulkConfig.UnderlyingConnection = _ => connection;
            bulkConfig.UnderlyingTransaction = _ => transaction;
        }

        return bulkConfig;
    }

    /// <summary>
    /// Updates NOIR config with stats from the completed operation.
    /// </summary>
    /// <param name="config">The NOIR configuration to update.</param>
    /// <param name="bulkConfig">The EFCore.BulkExtensions config with stats.</param>
    /// <param name="stopwatch">Stopwatch measuring operation duration.</param>
    /// <param name="entityCount">Number of entities processed (for batch calculation).</param>
    public static void UpdateStats(BulkOperationConfig? config, BulkConfig bulkConfig, Stopwatch stopwatch, int entityCount = 0)
    {
        if (config == null || !config.CalculateStats)
            return;

        var batchSize = config.BatchSize > 0 ? config.BatchSize : 2000;
        var batchesProcessed = entityCount > 0
            ? (int)Math.Ceiling((double)entityCount / batchSize)
            : 0;

        config.Stats = new BulkOperationStats
        {
            RowsInserted = bulkConfig.StatsInfo?.StatsNumberInserted ?? 0,
            RowsUpdated = bulkConfig.StatsInfo?.StatsNumberUpdated ?? 0,
            RowsDeleted = bulkConfig.StatsInfo?.StatsNumberDeleted ?? 0,
            Duration = stopwatch.Elapsed,
            BatchesProcessed = batchesProcessed
        };
    }
}
