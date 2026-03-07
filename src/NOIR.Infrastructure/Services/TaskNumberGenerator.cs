namespace NOIR.Infrastructure.Services;

/// <summary>
/// Generates unique task numbers using atomic database-level increment.
/// Uses SequenceCounters table with row-level locking for thread safety.
/// Format: {projectPrefix}-NNN (e.g., "PROJ-001").
/// </summary>
public class TaskNumberGenerator : ITaskNumberGenerator, IScopedService
{
    // Direct DbContext injection is required here as an exception to the standard IUnitOfWork pattern.
    // Reason: Database.SqlQueryRaw<int>() (needed for the MERGE atomic upsert) is only available
    // on DbContext.Database, not through IUnitOfWork or IRepository abstractions.
    private readonly ApplicationDbContext _dbContext;

    public TaskNumberGenerator(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> GenerateNextAsync(string projectPrefix, string? tenantId, CancellationToken cancellationToken = default)
    {
        var prefix = $"{projectPrefix}-";

        // Use raw SQL for atomic upsert + increment
        var nextValue = await AtomicIncrementAsync(prefix, tenantId, cancellationToken);

        // D3 format supports up to 999 tasks per project prefix (auto-extends beyond)
        return $"{prefix}{nextValue:D3}";
    }

    private async Task<int> AtomicIncrementAsync(string prefix, string? tenantId, CancellationToken cancellationToken)
    {
        var sql = tenantId != null
            ? @"
                MERGE INTO SequenceCounters WITH (HOLDLOCK) AS target
                USING (SELECT {0} AS Prefix, {1} AS TenantId) AS source
                ON target.Prefix = source.Prefix AND target.TenantId = source.TenantId
                WHEN MATCHED THEN
                    UPDATE SET CurrentValue = target.CurrentValue + 1
                WHEN NOT MATCHED THEN
                    INSERT (Id, TenantId, Prefix, CurrentValue)
                    VALUES (NEWID(), {1}, {0}, 1)
                OUTPUT INSERTED.CurrentValue;"
            : @"
                MERGE INTO SequenceCounters WITH (HOLDLOCK) AS target
                USING (SELECT {0} AS Prefix) AS source
                ON target.Prefix = source.Prefix AND target.TenantId IS NULL
                WHEN MATCHED THEN
                    UPDATE SET CurrentValue = target.CurrentValue + 1
                WHEN NOT MATCHED THEN
                    INSERT (Id, TenantId, Prefix, CurrentValue)
                    VALUES (NEWID(), NULL, {0}, 1)
                OUTPUT INSERTED.CurrentValue;";

        // Use ToListAsync() instead of FirstAsync() to avoid EF Core non-composable SQL error
        var results = tenantId != null
            ? await _dbContext.Database.SqlQueryRaw<int>(sql, prefix, tenantId).ToListAsync(cancellationToken)
            : await _dbContext.Database.SqlQueryRaw<int>(sql, prefix).ToListAsync(cancellationToken);

        return results.First();
    }
}
