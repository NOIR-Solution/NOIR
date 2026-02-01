namespace NOIR.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for ShippingWebhookLog entities.
/// Note: This entity is NOT tenant-scoped, uses base Repository without tenant filtering.
/// </summary>
public sealed class ShippingWebhookLogRepository : IScopedService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IDateTime _dateTime;

    public ShippingWebhookLogRepository(
        ApplicationDbContext dbContext,
        IDateTime dateTime)
    {
        _dbContext = dbContext;
        _dateTime = dateTime;
    }

    public async Task<ShippingWebhookLog?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbContext.ShippingWebhookLogs
            .TagWith("GetByIdAsync")
            .FirstOrDefaultAsync(l => l.Id == id, ct);
    }

    public async Task AddAsync(ShippingWebhookLog log, CancellationToken ct = default)
    {
        await _dbContext.ShippingWebhookLogs.AddAsync(log, ct);
    }

    public void Update(ShippingWebhookLog log)
    {
        _dbContext.ShippingWebhookLogs.Update(log);
    }

    public async Task<List<ShippingWebhookLog>> GetUnprocessedAsync(int maxAttempts = 3, int take = 100, CancellationToken ct = default)
    {
        return await _dbContext.ShippingWebhookLogs
            .TagWith("GetUnprocessedAsync")
            .Where(l => !l.ProcessedSuccessfully && l.ProcessingAttempts < maxAttempts)
            .OrderBy(l => l.ReceivedAt)
            .Take(take)
            .ToListAsync(ct);
    }

    public async Task<List<ShippingWebhookLog>> GetByTrackingNumberAsync(string trackingNumber, CancellationToken ct = default)
    {
        return await _dbContext.ShippingWebhookLogs
            .TagWith("GetByTrackingNumberAsync")
            .Where(l => l.TrackingNumber == trackingNumber)
            .OrderByDescending(l => l.ReceivedAt)
            .ToListAsync(ct);
    }
}
