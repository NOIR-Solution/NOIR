using System.Text.Json;
using Microsoft.Extensions.Options;

namespace NOIR.Infrastructure.Audit;

/// <summary>
/// Hangfire job for audit log retention policy enforcement.
/// Handles archiving and deletion of old audit logs based on configuration.
/// </summary>
public class AuditRetentionJob : IScopedService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IFileStorage _fileStorage;
    private readonly AuditRetentionSettings _settings;
    private readonly ILogger<AuditRetentionJob> _logger;
    private readonly IDateTime _dateTime;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AuditRetentionJob(
        ApplicationDbContext dbContext,
        IFileStorage fileStorage,
        IOptions<AuditRetentionSettings> settings,
        ILogger<AuditRetentionJob> logger,
        IDateTime dateTime)
    {
        _dbContext = dbContext;
        _fileStorage = fileStorage;
        _settings = settings.Value;
        _logger = logger;
        _dateTime = dateTime;
    }

    /// <summary>
    /// Main job entry point. Called by Hangfire on schedule.
    /// </summary>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Audit retention job is disabled, skipping execution");
            return;
        }

        // Validate configuration to prevent data loss from misconfiguration
        ValidateConfiguration();

        _logger.LogInformation("Starting audit retention job");
        var sw = Stopwatch.StartNew();

        try
        {
            var now = _dateTime.UtcNow;
            var archiveCutoff = now.AddDays(-_settings.ArchiveAfterDays);
            var deleteCutoff = now.AddDays(-_settings.DeleteAfterDays);

            // Step 1: Delete old archived records (oldest first)
            var deletedCount = await DeleteOldRecordsAsync(deleteCutoff, cancellationToken);

            // Step 2: Archive eligible records
            var archivedCount = 0;
            if (_settings.EnableArchiving)
            {
                archivedCount = await ArchiveRecordsAsync(archiveCutoff, cancellationToken);
            }
            else
            {
                // Direct deletion without archiving
                deletedCount += await DeleteRecordsDirectlyAsync(archiveCutoff, cancellationToken);
            }

            sw.Stop();
            _logger.LogInformation(
                "Audit retention job completed in {ElapsedMs}ms. Archived: {Archived}, Deleted: {Deleted}",
                sw.ElapsedMilliseconds, archivedCount, deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Audit retention job failed");
            throw;
        }
    }

    /// <summary>
    /// Archives old audit records by marking them as archived.
    /// </summary>
    private async Task<int> ArchiveRecordsAsync(DateTimeOffset cutoff, CancellationToken ct)
    {
        var totalArchived = 0;

        // Archive HttpRequestAuditLogs
        var httpLogsToArchive = await _dbContext.HttpRequestAuditLogs
            .TagWith("AuditRetentionJob.ArchiveHttpLogs")
            .Where(h => h.StartTime < cutoff && !h.IsArchived)
            .OrderBy(h => h.StartTime)
            .Take(_settings.BatchSize)
            .ToListAsync(ct);

        if (httpLogsToArchive.Count > 0)
        {
            // Export before archiving if enabled
            if (_settings.ExportBeforeDelete)
            {
                await ExportToStorageAsync("http-requests", httpLogsToArchive, ct);
            }

            foreach (var log in httpLogsToArchive)
            {
                log.IsArchived = true;
                log.ArchivedAt = _dateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync(ct);
            totalArchived += httpLogsToArchive.Count;

            _logger.LogInformation("Archived {Count} HTTP request audit logs", httpLogsToArchive.Count);
        }

        // Archive HandlerAuditLogs
        var handlerLogsToArchive = await _dbContext.HandlerAuditLogs
            .TagWith("AuditRetentionJob.ArchiveHandlerLogs")
            .Where(h => h.StartTime < cutoff && !h.IsArchived)
            .OrderBy(h => h.StartTime)
            .Take(_settings.BatchSize)
            .ToListAsync(ct);

        if (handlerLogsToArchive.Count > 0)
        {
            if (_settings.ExportBeforeDelete)
            {
                await ExportToStorageAsync("handlers", handlerLogsToArchive, ct);
            }

            foreach (var log in handlerLogsToArchive)
            {
                log.IsArchived = true;
                log.ArchivedAt = _dateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync(ct);
            totalArchived += handlerLogsToArchive.Count;

            _logger.LogInformation("Archived {Count} handler audit logs", handlerLogsToArchive.Count);
        }

        // Archive EntityAuditLogs
        var entityLogsToArchive = await _dbContext.EntityAuditLogs
            .TagWith("AuditRetentionJob.ArchiveEntityLogs")
            .Where(e => e.Timestamp < cutoff && !e.IsArchived)
            .OrderBy(e => e.Timestamp)
            .Take(_settings.BatchSize)
            .ToListAsync(ct);

        if (entityLogsToArchive.Count > 0)
        {
            if (_settings.ExportBeforeDelete)
            {
                await ExportToStorageAsync("entities", entityLogsToArchive, ct);
            }

            foreach (var log in entityLogsToArchive)
            {
                log.IsArchived = true;
                log.ArchivedAt = _dateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync(ct);
            totalArchived += entityLogsToArchive.Count;

            _logger.LogInformation("Archived {Count} entity audit logs", entityLogsToArchive.Count);
        }

        return totalArchived;
    }

    /// <summary>
    /// Deletes records that have been archived and exceed the deletion threshold.
    /// Uses optimized pattern: fetch IDs outside transaction, delete in separate short transactions.
    /// This prevents long-running transactions that can cause deadlocks.
    /// </summary>
    private async Task<int> DeleteOldRecordsAsync(DateTimeOffset cutoff, CancellationToken ct)
    {
        // STEP 1: Fetch all entity IDs to delete OUTSIDE transaction (no locks held)
        var entityIdsToDelete = await _dbContext.EntityAuditLogs
            .TagWith("AuditRetentionJob.DeleteEntityLogs_FetchIds")
            .AsNoTracking()
            .Where(e => e.IsArchived && e.ArchivedAt < cutoff)
            .OrderBy(e => e.Timestamp)
            .Take(_settings.BatchSize)
            .Select(e => e.Id)
            .ToListAsync(ct);

        // Use LEFT JOIN pattern instead of Any() subquery for better performance
        var handlerIdsToDelete = await _dbContext.HandlerAuditLogs
            .TagWith("AuditRetentionJob.DeleteHandlerLogs_FetchIds")
            .AsNoTracking()
            .Where(h => h.IsArchived && h.ArchivedAt < cutoff)
            .GroupJoin(
                _dbContext.EntityAuditLogs.AsNoTracking(),
                h => h.Id,
                e => e.HandlerAuditLogId,
                (h, entities) => new { Handler = h, HasEntities = entities.Any() })
            .Where(x => !x.HasEntities)
            .OrderBy(x => x.Handler.StartTime)
            .Take(_settings.BatchSize)
            .Select(x => x.Handler.Id)
            .ToListAsync(ct);

        var httpIdsToDelete = await _dbContext.HttpRequestAuditLogs
            .TagWith("AuditRetentionJob.DeleteHttpLogs_FetchIds")
            .AsNoTracking()
            .Where(h => h.IsArchived && h.ArchivedAt < cutoff)
            .GroupJoin(
                _dbContext.HandlerAuditLogs.AsNoTracking(),
                h => h.Id,
                hl => hl.HttpRequestAuditLogId,
                (h, handlers) => new { Http = h, HasHandlers = handlers.Any() })
            .Where(x => !x.HasHandlers)
            .OrderBy(x => x.Http.StartTime)
            .Take(_settings.BatchSize)
            .Select(x => x.Http.Id)
            .ToListAsync(ct);

        // Early exit if nothing to delete
        if (entityIdsToDelete.Count == 0 && handlerIdsToDelete.Count == 0 && httpIdsToDelete.Count == 0)
            return 0;

        var totalDeleted = 0;

        // STEP 2: Delete in separate short transactions to reduce lock contention
        // Delete child records first (EntityAuditLogs)
        if (entityIdsToDelete.Count > 0)
        {
            totalDeleted += await ExecuteDeleteWithRetryAsync(
                () => _dbContext.EntityAuditLogs
                    .Where(e => entityIdsToDelete.Contains(e.Id))
                    .ExecuteDeleteAsync(ct),
                "EntityAuditLogs",
                ct);
        }

        // Delete HandlerAuditLogs
        if (handlerIdsToDelete.Count > 0)
        {
            totalDeleted += await ExecuteDeleteWithRetryAsync(
                () => _dbContext.HandlerAuditLogs
                    .Where(h => handlerIdsToDelete.Contains(h.Id))
                    .ExecuteDeleteAsync(ct),
                "HandlerAuditLogs",
                ct);
        }

        // Delete HttpRequestAuditLogs
        if (httpIdsToDelete.Count > 0)
        {
            totalDeleted += await ExecuteDeleteWithRetryAsync(
                () => _dbContext.HttpRequestAuditLogs
                    .Where(h => httpIdsToDelete.Contains(h.Id))
                    .ExecuteDeleteAsync(ct),
                "HttpRequestAuditLogs",
                ct);
        }

        _logger.LogInformation(
            "Deleted {Total} archived audit logs (Entity: {Entity}, Handler: {Handler}, HTTP: {Http})",
            totalDeleted, entityIdsToDelete.Count, handlerIdsToDelete.Count, httpIdsToDelete.Count);

        return totalDeleted;
    }

    /// <summary>
    /// Executes a delete operation with retry logic for transient failures (deadlocks, timeouts).
    /// </summary>
    private async Task<int> ExecuteDeleteWithRetryAsync(
        Func<Task<int>> deleteOperation,
        string tableName,
        CancellationToken ct,
        int maxRetries = 3)
    {
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await deleteOperation();
            }
            catch (Exception ex) when (attempt < maxRetries && IsTransientError(ex))
            {
                _logger.LogWarning(
                    ex,
                    "Transient error deleting from {Table}, retry {Attempt}/{MaxRetries}",
                    tableName, attempt, maxRetries);

                // Exponential backoff: 100ms, 200ms, 400ms
                await Task.Delay(TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt - 1)), ct);
            }
        }

        // Final attempt without catch - let it throw
        return await deleteOperation();
    }

    /// <summary>
    /// Determines if an exception is a transient error that can be retried.
    /// </summary>
    private static bool IsTransientError(Exception ex)
    {
        var message = ex.Message;
        return message.Contains("deadlock", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("lock request time out", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Deletes records directly without archiving (when archiving is disabled).
    /// Uses optimized pattern: fetch IDs outside transaction, delete in separate short transactions.
    /// This prevents long-running transactions that can cause deadlocks.
    /// </summary>
    private async Task<int> DeleteRecordsDirectlyAsync(DateTimeOffset cutoff, CancellationToken ct)
    {
        // STEP 1: Fetch all entity IDs to delete OUTSIDE transaction (no locks held)
        var entityIdsToDelete = await _dbContext.EntityAuditLogs
            .TagWith("AuditRetentionJob.DirectDeleteEntityLogs_FetchIds")
            .AsNoTracking()
            .Where(e => e.Timestamp < cutoff)
            .OrderBy(e => e.Timestamp)
            .Take(_settings.BatchSize)
            .Select(e => e.Id)
            .ToListAsync(ct);

        // Use LEFT JOIN pattern instead of Any() subquery for better performance
        var handlerIdsToDelete = await _dbContext.HandlerAuditLogs
            .TagWith("AuditRetentionJob.DirectDeleteHandlerLogs_FetchIds")
            .AsNoTracking()
            .Where(h => h.StartTime < cutoff)
            .GroupJoin(
                _dbContext.EntityAuditLogs.AsNoTracking(),
                h => h.Id,
                e => e.HandlerAuditLogId,
                (h, entities) => new { Handler = h, HasEntities = entities.Any() })
            .Where(x => !x.HasEntities)
            .OrderBy(x => x.Handler.StartTime)
            .Take(_settings.BatchSize)
            .Select(x => x.Handler.Id)
            .ToListAsync(ct);

        var httpIdsToDelete = await _dbContext.HttpRequestAuditLogs
            .TagWith("AuditRetentionJob.DirectDeleteHttpLogs_FetchIds")
            .AsNoTracking()
            .Where(h => h.StartTime < cutoff)
            .GroupJoin(
                _dbContext.HandlerAuditLogs.AsNoTracking(),
                h => h.Id,
                hl => hl.HttpRequestAuditLogId,
                (h, handlers) => new { Http = h, HasHandlers = handlers.Any() })
            .Where(x => !x.HasHandlers)
            .OrderBy(x => x.Http.StartTime)
            .Take(_settings.BatchSize)
            .Select(x => x.Http.Id)
            .ToListAsync(ct);

        // Early exit if nothing to delete
        if (entityIdsToDelete.Count == 0 && handlerIdsToDelete.Count == 0 && httpIdsToDelete.Count == 0)
            return 0;

        var totalDeleted = 0;

        // STEP 2: Delete in separate short transactions to reduce lock contention
        // Delete child records first (EntityAuditLogs)
        if (entityIdsToDelete.Count > 0)
        {
            totalDeleted += await ExecuteDeleteWithRetryAsync(
                () => _dbContext.EntityAuditLogs
                    .Where(e => entityIdsToDelete.Contains(e.Id))
                    .ExecuteDeleteAsync(ct),
                "EntityAuditLogs",
                ct);
        }

        // Delete HandlerAuditLogs
        if (handlerIdsToDelete.Count > 0)
        {
            totalDeleted += await ExecuteDeleteWithRetryAsync(
                () => _dbContext.HandlerAuditLogs
                    .Where(h => handlerIdsToDelete.Contains(h.Id))
                    .ExecuteDeleteAsync(ct),
                "HandlerAuditLogs",
                ct);
        }

        // Delete HttpRequestAuditLogs
        if (httpIdsToDelete.Count > 0)
        {
            totalDeleted += await ExecuteDeleteWithRetryAsync(
                () => _dbContext.HttpRequestAuditLogs
                    .Where(h => httpIdsToDelete.Contains(h.Id))
                    .ExecuteDeleteAsync(ct),
                "HttpRequestAuditLogs",
                ct);
        }

        _logger.LogInformation(
            "Direct deleted {Total} audit logs (Entity: {Entity}, Handler: {Handler}, HTTP: {Http})",
            totalDeleted, entityIdsToDelete.Count, handlerIdsToDelete.Count, httpIdsToDelete.Count);

        return totalDeleted;
    }

    /// <summary>
    /// Exports records to file storage before archiving/deletion.
    /// Uses streaming to avoid loading entire JSON into memory.
    /// </summary>
    private async Task ExportToStorageAsync<T>(string prefix, IReadOnlyList<T> records, CancellationToken ct)
    {
        if (records.Count == 0) return;

        var fileName = $"{Guid.NewGuid():N}.json";
        var folder = $"{_settings.ExportPath}/{prefix}/{_dateTime.UtcNow:yyyy/MM/dd}";

        // Stream directly to memory stream to avoid double memory allocation
        using var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, records, JsonOptions, ct);

        stream.Position = 0; // Reset for reading
        var path = await _fileStorage.UploadAsync(fileName, stream, folder, ct);

        _logger.LogInformation("Exported {Count} {Type} records to {Path}", records.Count, prefix, path);
    }

    /// <summary>
    /// Validates the retention configuration to prevent data loss from misconfiguration.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when configuration is invalid.</exception>
    private void ValidateConfiguration()
    {
        // Validate non-negative values
        if (_settings.ArchiveAfterDays < 0)
        {
            throw new InvalidOperationException(
                $"ArchiveAfterDays ({_settings.ArchiveAfterDays}) cannot be negative.");
        }

        if (_settings.DeleteAfterDays < 0)
        {
            throw new InvalidOperationException(
                $"DeleteAfterDays ({_settings.DeleteAfterDays}) cannot be negative.");
        }

        if (_settings.DeleteAfterDays <= _settings.ArchiveAfterDays)
        {
            throw new InvalidOperationException(
                $"DeleteAfterDays ({_settings.DeleteAfterDays}) must be greater than ArchiveAfterDays ({_settings.ArchiveAfterDays}) " +
                "to ensure records are archived before deletion.");
        }

        if (_settings.BatchSize <= 0)
        {
            throw new InvalidOperationException(
                $"BatchSize ({_settings.BatchSize}) must be greater than 0.");
        }

        if (_settings.BatchSize > 10000)
        {
            _logger.LogWarning(
                "BatchSize ({BatchSize}) is very large, this may cause memory issues",
                _settings.BatchSize);
        }

        if (_settings.ArchiveAfterDays < 30)
        {
            _logger.LogWarning(
                "ArchiveAfterDays ({Days}) is less than 30 days, audit logs will be archived quickly",
                _settings.ArchiveAfterDays);
        }

        if (_settings.DeleteAfterDays < 90)
        {
            _logger.LogWarning(
                "DeleteAfterDays ({Days}) is less than 90 days, this may not meet compliance requirements",
                _settings.DeleteAfterDays);
        }
    }
}
