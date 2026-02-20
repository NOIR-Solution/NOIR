using NOIR.Domain.Entities.Common;

namespace NOIR.Infrastructure.Services;

/// <summary>
/// Generates unique order numbers using atomic database-level increment.
/// Uses SequenceCounters table with row-level locking for thread safety.
/// </summary>
public class OrderNumberGenerator : IOrderNumberGenerator, IScopedService
{
    // Direct DbContext injection is required here as an exception to the standard IUnitOfWork pattern.
    // Reason: Database.SqlQueryRaw<int>() (needed for the MERGE atomic upsert) is only available
    // on DbContext.Database, not through IUnitOfWork or IRepository abstractions.
    private readonly ApplicationDbContext _dbContext;

    public OrderNumberGenerator(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> GenerateNextAsync(string? tenantId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow;
        var prefix = $"ORD-{today:yyyyMMdd}-";

        // Use raw SQL for atomic upsert + increment
        // This is a single atomic operation - no race conditions possible
        var nextValue = await AtomicIncrementAsync(prefix, tenantId, cancellationToken);

        // D6 format supports up to 999,999 orders per day per tenant
        return $"{prefix}{nextValue:D6}";
    }

    private async Task<int> AtomicIncrementAsync(string prefix, string? tenantId, CancellationToken cancellationToken)
    {
        // Use MERGE (upsert) with OUTPUT for atomic increment
        // If row exists: increment CurrentValue and return new value
        // If row doesn't exist: insert with CurrentValue=1 and return 1
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

        var result = tenantId != null
            ? await _dbContext.Database.SqlQueryRaw<int>(sql, prefix, tenantId).FirstAsync(cancellationToken)
            : await _dbContext.Database.SqlQueryRaw<int>(sql, prefix).FirstAsync(cancellationToken);

        return result;
    }
}
