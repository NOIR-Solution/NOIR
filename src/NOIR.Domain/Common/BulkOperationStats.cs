namespace NOIR.Domain.Common;

/// <summary>
/// Statistics from a bulk operation.
/// Populated when BulkOperationConfig.CalculateStats is true.
/// </summary>
public class BulkOperationStats
{
    /// <summary>
    /// Number of records inserted.
    /// </summary>
    public int RowsInserted { get; set; }

    /// <summary>
    /// Number of records updated.
    /// </summary>
    public int RowsUpdated { get; set; }

    /// <summary>
    /// Number of records deleted.
    /// </summary>
    public int RowsDeleted { get; set; }

    /// <summary>
    /// Total number of records affected (inserted + updated + deleted).
    /// </summary>
    public int TotalRowsAffected => RowsInserted + RowsUpdated + RowsDeleted;

    /// <summary>
    /// Duration of the operation.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Number of batches processed.
    /// </summary>
    public int BatchesProcessed { get; set; }

    public override string ToString()
    {
        return $"Bulk Operation: {TotalRowsAffected} rows affected " +
               $"(Inserted: {RowsInserted}, Updated: {RowsUpdated}, Deleted: {RowsDeleted}) " +
               $"in {Duration.TotalMilliseconds:F0}ms ({BatchesProcessed} batches)";
    }
}
