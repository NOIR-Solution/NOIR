namespace NOIR.Infrastructure.Persistence.Interceptors;

/// <summary>
/// EF Core interceptor that sets TenantId on ITenantEntity before saving.
/// Works with Finbuckle.MultiTenant to automatically tag entities with the current tenant.
/// </summary>
public class TenantIdSetterInterceptor : SaveChangesInterceptor
{
    private readonly IMultiTenantContextAccessor<Tenant> _tenantContextAccessor;
    private readonly ILogger<TenantIdSetterInterceptor> _logger;

    public TenantIdSetterInterceptor(
        IMultiTenantContextAccessor<Tenant> tenantContextAccessor,
        ILogger<TenantIdSetterInterceptor> logger)
    {
        _tenantContextAccessor = tenantContextAccessor;
        _logger = logger;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        SetTenantId(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        SetTenantId(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void SetTenantId(DbContext? context)
    {
        if (context == null) return;

        var tenantId = _tenantContextAccessor.MultiTenantContext?.TenantInfo?.Id;

        // Log tenant context state
        _logger.LogDebug("[TenantIdInterceptor] Current tenant context: {TenantId}", tenantId ?? "NULL");

        if (string.IsNullOrEmpty(tenantId))
        {
            _logger.LogDebug("[TenantIdInterceptor] No tenant context set, skipping TenantId assignment");
            return;
        }

        var entities = context.ChangeTracker.Entries<ITenantEntity>().ToList();
        _logger.LogDebug("[TenantIdInterceptor] Processing {Count} ITenantEntity entries", entities.Count);

        foreach (var entry in entities)
        {
            var entityType = entry.Entity.GetType().Name;
            var entityId = entry.Entity switch
            {
                ApplicationUser u => u.Email,
                _ => entry.Property("Id")?.CurrentValue?.ToString() ?? "unknown"
            };

            _logger.LogDebug(
                "[TenantIdInterceptor] Entity: {EntityType} ({EntityId}), State: {State}, CurrentTenantId: {CurrentTenantId}",
                entityType,
                entityId,
                entry.State,
                entry.Entity.TenantId ?? "NULL");

            // CRITICAL: Never modify TenantId for system users - they must remain tenant-agnostic
            if (entry.Entity is ApplicationUser user && user.IsSystemUser)
            {
                _logger.LogInformation(
                    "[TenantIdInterceptor] SKIPPING system user: {Email} (IsSystemUser=true, TenantId={TenantId})",
                    user.Email,
                    user.TenantId ?? "NULL");
                continue;
            }

            if (entry.State == EntityState.Added && string.IsNullOrEmpty(entry.Entity.TenantId))
            {
                // Use EF Core property API to set the value, bypassing the protected setter
                // This is the only place that should set TenantId after construction
                entry.Property(nameof(ITenantEntity.TenantId)).CurrentValue = tenantId;

                _logger.LogInformation(
                    "[TenantIdInterceptor] SET TenantId={TenantId} for {EntityType} ({EntityId})",
                    tenantId,
                    entityType,
                    entityId);
            }
            // Don't change TenantId on updates - entities should stay in their original tenant
        }
    }
}
