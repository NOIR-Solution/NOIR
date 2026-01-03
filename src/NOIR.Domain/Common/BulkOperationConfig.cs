namespace NOIR.Domain.Common;

/// <summary>
/// Configuration options for bulk operations.
/// Provides a simplified, library-agnostic API for configuring bulk operations.
/// Maps to EFCore.BulkExtensions.BulkConfig internally.
/// </summary>
public class BulkOperationConfig
{
    /// <summary>
    /// Number of records per batch. Default is 2000.
    /// Larger batches may improve performance but use more memory.
    /// Recommended: 1000-5000 depending on record size.
    /// </summary>
    public int BatchSize { get; set; } = 2000;

    /// <summary>
    /// Timeout in seconds for the bulk copy operation.
    /// Null uses the default (30 seconds). Set to 0 for no timeout.
    /// </summary>
    public int? BulkCopyTimeout { get; set; }

    /// <summary>
    /// When true, retrieves database-generated identity values after insert.
    /// Has performance cost - only enable when you need the generated IDs.
    /// </summary>
    public bool SetOutputIdentity { get; set; }

    /// <summary>
    /// When true, maintains the order of entities during insert.
    /// Required when SetOutputIdentity is true to correctly map IDs back.
    /// </summary>
    public bool PreserveInsertOrder { get; set; } = true;

    /// <summary>
    /// List of property names to include in the operation.
    /// When set, only these properties are affected.
    /// Cannot be used together with PropertiesToExclude.
    /// </summary>
    public IList<string>? PropertiesToInclude { get; set; }

    /// <summary>
    /// List of property names to exclude from the operation.
    /// When set, these properties are not affected.
    /// Cannot be used together with PropertiesToInclude.
    /// </summary>
    public IList<string>? PropertiesToExclude { get; set; }

    /// <summary>
    /// Properties used to match entities for update/upsert operations.
    /// When not set, primary key is used for matching.
    /// Example: ["Email", "TenantId"] for matching by business key.
    /// </summary>
    public IList<string>? UpdateByProperties { get; set; }

    /// <summary>
    /// When true, calculates and returns operation statistics.
    /// </summary>
    public bool CalculateStats { get; set; }

    /// <summary>
    /// Statistics from the bulk operation. Populated when CalculateStats is true.
    /// </summary>
    public BulkOperationStats? Stats { get; set; }

    /// <summary>
    /// When true, uses HOLDLOCK for the MERGE operation (SQL Server).
    /// Provides serializable isolation but may cause deadlocks with concurrent operations.
    /// Default is true for data consistency.
    /// </summary>
    public bool WithHoldlock { get; set; } = true;

    /// <summary>
    /// When true, includes navigation properties in the operation (experimental).
    /// Use with caution - may have unexpected behavior with complex object graphs.
    /// </summary>
    public bool IncludeGraph { get; set; }

    /// <summary>
    /// REQUIRED for BulkSyncAsync. Must be set to true to confirm you understand
    /// that records NOT in the collection will be PERMANENTLY DELETED.
    /// This is a safety mechanism to prevent accidental data loss.
    /// </summary>
    public bool ConfirmSyncWillDeleteMissingRecords { get; set; }

    /// <summary>
    /// Additional safety for BulkSyncAsync with empty collection.
    /// When true, confirms that passing an empty collection (which deletes ALL records) is intentional.
    /// </summary>
    public bool ConfirmSyncWithEmptyCollection { get; set; }

    #region Fluent API

    /// <summary>
    /// Sets the batch size for the operation.
    /// </summary>
    public BulkOperationConfig WithBatchSize(int batchSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);
        BatchSize = batchSize;
        return this;
    }

    /// <summary>
    /// Enables output identity retrieval (with performance cost).
    /// </summary>
    public BulkOperationConfig WithIdentityOutput()
    {
        SetOutputIdentity = true;
        PreserveInsertOrder = true;
        return this;
    }

    /// <summary>
    /// Specifies properties to include in the operation.
    /// </summary>
    public BulkOperationConfig IncludeProperties(params string[] properties)
    {
        PropertiesToInclude = properties.ToList();
        return this;
    }

    /// <summary>
    /// Specifies properties to exclude from the operation.
    /// </summary>
    public BulkOperationConfig ExcludeProperties(params string[] properties)
    {
        PropertiesToExclude = properties.ToList();
        return this;
    }

    /// <summary>
    /// Specifies properties to use for matching during update/upsert.
    /// </summary>
    public BulkOperationConfig UpdateBy(params string[] properties)
    {
        UpdateByProperties = properties.ToList();
        return this;
    }

    /// <summary>
    /// Enables statistics calculation.
    /// </summary>
    public BulkOperationConfig WithStats()
    {
        CalculateStats = true;
        return this;
    }

    /// <summary>
    /// Sets the bulk copy timeout in seconds.
    /// </summary>
    public BulkOperationConfig WithTimeout(int seconds)
    {
        BulkCopyTimeout = seconds;
        return this;
    }

    /// <summary>
    /// Disables HOLDLOCK for the MERGE operation (reduces deadlock risk but may reduce consistency).
    /// </summary>
    public BulkOperationConfig WithoutHoldlock()
    {
        WithHoldlock = false;
        return this;
    }

    /// <summary>
    /// Confirms sync operation will delete missing records.
    /// Required for BulkSyncAsync.
    /// </summary>
    public BulkOperationConfig ConfirmSyncDeletion()
    {
        ConfirmSyncWillDeleteMissingRecords = true;
        return this;
    }

    #endregion

    /// <summary>
    /// Creates a default configuration optimized for performance.
    /// SetOutputIdentity = false, BatchSize = 2000
    /// </summary>
    public static BulkOperationConfig Default => new();

    /// <summary>
    /// Creates a configuration optimized for insert operations that need generated IDs.
    /// SetOutputIdentity = true, PreserveInsertOrder = true
    /// </summary>
    public static BulkOperationConfig WithOutputIdentity => new()
    {
        SetOutputIdentity = true,
        PreserveInsertOrder = true
    };

    /// <summary>
    /// Creates a configuration for large batch operations.
    /// BatchSize = 5000, BulkCopyTimeout = 120
    /// </summary>
    public static BulkOperationConfig LargeBatch => new()
    {
        BatchSize = 5000,
        BulkCopyTimeout = 120
    };
}
