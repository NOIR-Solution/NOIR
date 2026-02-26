namespace NOIR.Infrastructure.Customers;

/// <summary>
/// Hangfire recurring job that recalculates customer segments based on RFM data.
/// Runs daily for each tenant with the Customers feature enabled.
/// Resolves repositories per-tenant scope to ensure correct multi-tenant data isolation.
/// </summary>
public class CustomerSegmentationJob : IScopedService
{
    private readonly TenantStoreDbContext _tenantStore;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CustomerSegmentationJob> _logger;

    public CustomerSegmentationJob(
        TenantStoreDbContext tenantStore,
        IServiceScopeFactory scopeFactory,
        ILogger<CustomerSegmentationJob> logger)
    {
        _tenantStore = tenantStore;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Main job entry point. Called by Hangfire on schedule.
    /// Iterates all active tenants and recalculates customer segments for each.
    /// </summary>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting CustomerSegmentationJob");
        var sw = Stopwatch.StartNew();
        var totalProcessed = 0;

        var tenants = await _tenantStore.TenantInfo
            .Where(t => t.IsActive && !t.IsDeleted)
            .TagWith("CustomerSegmentationJob:LoadActiveTenants")
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "CustomerSegmentationJob: Processing {Count} active tenants",
            tenants.Count);

        foreach (var tenant in tenants)
        {
            // Create a dedicated scope per tenant with proper Finbuckle context
            await using var scope = _scopeFactory.CreateAsyncScope();

            var tenantSetter = scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>();
            tenantSetter.MultiTenantContext = new MultiTenantContext<Tenant>(tenant);

            var featureChecker = scope.ServiceProvider.GetRequiredService<IFeatureChecker>();
            if (!await featureChecker.IsEnabledAsync(ModuleNames.Ecommerce.Customers, cancellationToken))
            {
                _logger.LogDebug(
                    "CustomerSegmentationJob: Skipping tenant {TenantId} ({TenantName}) — Customers feature not enabled",
                    tenant.Id, tenant.Name);
                continue;
            }

            try
            {
                var customerRepo = scope.ServiceProvider.GetRequiredService<IRepository<Customer, Guid>>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var processed = await ProcessTenantAsync(customerRepo, unitOfWork, tenant.Id!, cancellationToken);
                totalProcessed += processed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "CustomerSegmentationJob: Failed for tenant {TenantId} ({TenantName})",
                    tenant.Id, tenant.Name);
                // Continue to next tenant — don't let one failure stop all tenants
            }
        }

        sw.Stop();
        _logger.LogInformation(
            "CustomerSegmentationJob completed in {ElapsedMs}ms — {Total} customers processed across all tenants",
            sw.ElapsedMilliseconds, totalProcessed);
    }

    private async Task<int> ProcessTenantAsync(
        IRepository<Customer, Guid> customerRepo,
        IUnitOfWork unitOfWork,
        string tenantId,
        CancellationToken ct)
    {
        var tenantSw = Stopwatch.StartNew();

        var customers = await customerRepo.ListAsync(new AllActiveCustomersForSegmentationSpec(), ct);

        if (customers.Count == 0) return 0;

        var changedCount = 0;
        foreach (var customer in customers)
        {
            var prevSegment = customer.Segment;
            customer.RecalculateSegment();
            if (customer.Segment != prevSegment)
                changedCount++;
        }

        await unitOfWork.SaveChangesAsync(ct);
        tenantSw.Stop();

        _logger.LogInformation(
            "CustomerSegmentationJob: Tenant {TenantId} — {Total} processed, {Changed} segment changes in {ElapsedMs}ms",
            tenantId, customers.Count, changedCount, tenantSw.ElapsedMilliseconds);

        return customers.Count;
    }
}
