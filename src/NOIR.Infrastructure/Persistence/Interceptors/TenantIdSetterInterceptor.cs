namespace NOIR.Infrastructure.Persistence.Interceptors;

/// <summary>
/// EF Core interceptor that sets TenantId on ITenantEntity before saving.
/// Works with Finbuckle.MultiTenant to automatically tag entities with the current tenant.
/// </summary>
public class TenantIdSetterInterceptor : SaveChangesInterceptor
{
    private readonly IMultiTenantContextAccessor<Tenant> _tenantContextAccessor;

    public TenantIdSetterInterceptor(IMultiTenantContextAccessor<Tenant> tenantContextAccessor)
    {
        _tenantContextAccessor = tenantContextAccessor;
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
        if (string.IsNullOrEmpty(tenantId)) return;

        foreach (var entry in context.ChangeTracker.Entries<ITenantEntity>())
        {
            if (entry.State == EntityState.Added && string.IsNullOrEmpty(entry.Entity.TenantId))
            {
                // Skip system users (e.g., platform admin) - they have explicit null TenantId for cross-tenant access
                if (entry.Entity is ApplicationUser user && user.IsSystemUser)
                    continue;

                // Use EF Core property API to set the value, bypassing the protected setter
                // This is the only place that should set TenantId after construction
                entry.Property(nameof(ITenantEntity.TenantId)).CurrentValue = tenantId;
            }
            // Don't change TenantId on updates - entities should stay in their original tenant
        }
    }
}
