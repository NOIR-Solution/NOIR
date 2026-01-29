namespace NOIR.Infrastructure.Products;

/// <summary>
/// Hangfire job for ProductFilterIndex maintenance.
/// Detects and fixes stale or orphaned index entries.
/// Scheduled to run periodically (e.g., hourly).
/// </summary>
public class ProductFilterIndexMaintenanceJob : IScopedService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ProductFilterIndexReindexJob _reindexJob;
    private readonly ILogger<ProductFilterIndexMaintenanceJob> _logger;
    private readonly IDateTime _dateTime;

    private const int BatchSize = 100;
    private static readonly TimeSpan StaleThreshold = TimeSpan.FromMinutes(5);

    public ProductFilterIndexMaintenanceJob(
        ApplicationDbContext dbContext,
        ProductFilterIndexReindexJob reindexJob,
        ILogger<ProductFilterIndexMaintenanceJob> logger,
        IDateTime dateTime)
    {
        _dbContext = dbContext;
        _reindexJob = reindexJob;
        _logger = logger;
        _dateTime = dateTime;
    }

    /// <summary>
    /// Main job entry point. Performs maintenance tasks on the filter index.
    /// </summary>
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting ProductFilterIndex maintenance job");
        var sw = Stopwatch.StartNew();

        try
        {
            // Step 1: Find and remove orphaned index entries (products deleted)
            var orphansRemoved = await RemoveOrphanedIndexesAsync(ct);

            // Step 2: Find and reindex stale entries
            var staleFixed = await FixStaleIndexesAsync(ct);

            // Step 3: Find products missing index entries and create them
            var missingCreated = await CreateMissingIndexesAsync(ct);

            sw.Stop();
            _logger.LogInformation(
                "ProductFilterIndex maintenance completed in {ElapsedMs}ms. " +
                "Orphans removed: {Orphans}, Stale fixed: {Stale}, Missing created: {Missing}",
                sw.ElapsedMilliseconds, orphansRemoved, staleFixed, missingCreated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ProductFilterIndex maintenance job failed");
            throw;
        }
    }

    /// <summary>
    /// Removes index entries for products that no longer exist.
    /// </summary>
    private async Task<int> RemoveOrphanedIndexesAsync(CancellationToken ct)
    {
        _logger.LogDebug("Checking for orphaned filter index entries");

        // Use LEFT JOIN to find indexes without matching products
        var orphanedIndexIds = await _dbContext.ProductFilterIndexes
            .TagWith("ProductFilterIndexMaintenanceJob.FindOrphans")
            .AsNoTracking()
            .GroupJoin(
                _dbContext.Products.AsNoTracking(),
                fi => fi.ProductId,
                p => p.Id,
                (fi, products) => new { FilterIndex = fi, HasProduct = products.Any() })
            .Where(x => !x.HasProduct)
            .Take(BatchSize)
            .Select(x => x.FilterIndex.Id)
            .ToListAsync(ct);

        if (orphanedIndexIds.Count == 0)
        {
            _logger.LogDebug("No orphaned filter index entries found");
            return 0;
        }

        var deleted = await _dbContext.ProductFilterIndexes
            .Where(fi => orphanedIndexIds.Contains(fi.Id))
            .ExecuteDeleteAsync(ct);

        _logger.LogInformation("Removed {Count} orphaned filter index entries", deleted);
        return deleted;
    }

    /// <summary>
    /// Finds and reindexes stale index entries.
    /// An entry is stale if LastSyncedAt < ProductUpdatedAt or significantly old.
    /// </summary>
    private async Task<int> FixStaleIndexesAsync(CancellationToken ct)
    {
        _logger.LogDebug("Checking for stale filter index entries");

        var staleThreshold = _dateTime.UtcNow.AddMinutes(-StaleThreshold.TotalMinutes);

        // Find stale indexes: either marked as stale or where sync timestamp is old
        var staleProductIds = await _dbContext.ProductFilterIndexes
            .TagWith("ProductFilterIndexMaintenanceJob.FindStale")
            .AsNoTracking()
            .Where(fi =>
                fi.LastSyncedAt < fi.ProductUpdatedAt ||  // Out of sync
                fi.LastSyncedAt == DateTime.MinValue)     // Explicitly marked stale
            .Take(BatchSize)
            .Select(fi => fi.ProductId)
            .ToListAsync(ct);

        if (staleProductIds.Count == 0)
        {
            _logger.LogDebug("No stale filter index entries found");
            return 0;
        }

        // Reindex each stale product
        var reindexed = 0;
        foreach (var productId in staleProductIds)
        {
            try
            {
                await _reindexJob.ReindexProductAsync(productId, ct);
                reindexed++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to reindex stale product {ProductId}", productId);
            }
        }

        _logger.LogInformation("Fixed {Count} stale filter index entries", reindexed);
        return reindexed;
    }

    /// <summary>
    /// Creates index entries for products that are missing them.
    /// </summary>
    private async Task<int> CreateMissingIndexesAsync(CancellationToken ct)
    {
        _logger.LogDebug("Checking for products missing filter index entries");

        // Find products without index entries using LEFT JOIN
        var missingProductIds = await _dbContext.Products
            .TagWith("ProductFilterIndexMaintenanceJob.FindMissing")
            .AsNoTracking()
            .GroupJoin(
                _dbContext.ProductFilterIndexes.AsNoTracking(),
                p => p.Id,
                fi => fi.ProductId,
                (p, indexes) => new { Product = p, HasIndex = indexes.Any() })
            .Where(x => !x.HasIndex)
            .Take(BatchSize)
            .Select(x => x.Product.Id)
            .ToListAsync(ct);

        if (missingProductIds.Count == 0)
        {
            _logger.LogDebug("No products missing filter index entries");
            return 0;
        }

        // Create index for each missing product
        var created = 0;
        foreach (var productId in missingProductIds)
        {
            try
            {
                await _reindexJob.ReindexProductAsync(productId, ct);
                created++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create index for product {ProductId}", productId);
            }
        }

        _logger.LogInformation("Created {Count} missing filter index entries", created);
        return created;
    }

    /// <summary>
    /// Gets statistics about the filter index health.
    /// Useful for monitoring and alerting.
    /// </summary>
    public async Task<FilterIndexHealthStats> GetHealthStatsAsync(CancellationToken ct = default)
    {
        var totalProducts = await _dbContext.Products
            .TagWith("ProductFilterIndexMaintenanceJob.HealthStats.Products")
            .AsNoTracking()
            .CountAsync(ct);

        var totalIndexes = await _dbContext.ProductFilterIndexes
            .TagWith("ProductFilterIndexMaintenanceJob.HealthStats.Indexes")
            .AsNoTracking()
            .CountAsync(ct);

        var staleCount = await _dbContext.ProductFilterIndexes
            .TagWith("ProductFilterIndexMaintenanceJob.HealthStats.Stale")
            .AsNoTracking()
            .Where(fi => fi.LastSyncedAt < fi.ProductUpdatedAt || fi.LastSyncedAt == DateTime.MinValue)
            .CountAsync(ct);

        // Find orphaned count
        var orphanedCount = await _dbContext.ProductFilterIndexes
            .TagWith("ProductFilterIndexMaintenanceJob.HealthStats.Orphans")
            .AsNoTracking()
            .GroupJoin(
                _dbContext.Products.AsNoTracking(),
                fi => fi.ProductId,
                p => p.Id,
                (fi, products) => new { HasProduct = products.Any() })
            .Where(x => !x.HasProduct)
            .CountAsync(ct);

        var missingCount = totalProducts - (totalIndexes - orphanedCount);

        return new FilterIndexHealthStats
        {
            TotalProducts = totalProducts,
            TotalIndexes = totalIndexes,
            StaleCount = staleCount,
            OrphanedCount = orphanedCount,
            MissingCount = Math.Max(0, missingCount),
            LastChecked = _dateTime.UtcNow
        };
    }
}

/// <summary>
/// Health statistics for the ProductFilterIndex.
/// </summary>
public record FilterIndexHealthStats
{
    public int TotalProducts { get; init; }
    public int TotalIndexes { get; init; }
    public int StaleCount { get; init; }
    public int OrphanedCount { get; init; }
    public int MissingCount { get; init; }
    public DateTimeOffset LastChecked { get; init; }

    public bool IsHealthy =>
        StaleCount == 0 &&
        OrphanedCount == 0 &&
        MissingCount == 0 &&
        TotalProducts == TotalIndexes;

    public double CoveragePercent =>
        TotalProducts > 0 ? (double)(TotalIndexes - OrphanedCount) / TotalProducts * 100 : 100;
}
