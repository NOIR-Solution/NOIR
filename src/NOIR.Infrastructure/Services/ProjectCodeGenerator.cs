namespace NOIR.Infrastructure.Services;

/// <summary>
/// Generates unique project codes using atomic database-level increment.
/// Uses SequenceCounters table with row-level locking for thread safety.
/// Format: PRJ-YYYYMMDD-NNNNNN
/// </summary>
public class ProjectCodeGenerator : IProjectCodeGenerator, IScopedService
{
    // Direct DbContext injection is required here as an exception to the standard IUnitOfWork pattern.
    // Reason: Database.SqlQueryRaw<int>() (needed for the MERGE atomic upsert) is only available
    // on DbContext.Database, not through IUnitOfWork or IRepository abstractions.
    private readonly ApplicationDbContext _dbContext;

    public ProjectCodeGenerator(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> GenerateNextAsync(string? tenantId, CancellationToken ct = default)
    {
        var today = DateTime.UtcNow;
        var prefix = $"PRJ-{today:yyyyMMdd}-";

        var nextValue = await AtomicIncrementAsync(prefix, tenantId, ct);

        return $"{prefix}{nextValue:D6}";
    }

    private async Task<int> AtomicIncrementAsync(string prefix, string? tenantId, CancellationToken ct)
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
            ? await _dbContext.Database.SqlQueryRaw<int>(sql, prefix, tenantId).ToListAsync(ct)
            : await _dbContext.Database.SqlQueryRaw<int>(sql, prefix).ToListAsync(ct);
        var result = results.First();

        return result;
    }
}
