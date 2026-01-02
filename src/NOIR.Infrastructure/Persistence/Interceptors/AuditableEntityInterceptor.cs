namespace NOIR.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Interceptor that automatically sets audit fields on entities.
/// </summary>
public class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUser _currentUser;
    private readonly IDateTime _dateTime;

    public AuditableEntityInterceptor(ICurrentUser currentUser, IDateTime dateTime)
    {
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateEntities(DbContext? context)
    {
        if (context is null) return;

        var utcNow = _dateTime.UtcNow;
        var userId = _currentUser.UserId;

        foreach (var entry in context.ChangeTracker.Entries<IAuditableEntity>())
        {
            // Convert hard delete to soft delete (data safety)
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Property(nameof(IAuditableEntity.IsDeleted)).CurrentValue = true;
                entry.Property(nameof(IAuditableEntity.DeletedAt)).CurrentValue = utcNow;
                entry.Property(nameof(IAuditableEntity.DeletedBy)).CurrentValue = userId;
                entry.Property(nameof(IAuditableEntity.ModifiedAt)).CurrentValue = utcNow;
                entry.Property(nameof(IAuditableEntity.ModifiedBy)).CurrentValue = userId;
                continue;
            }

            if (entry.State is EntityState.Added or EntityState.Modified || entry.HasChangedOwnedEntities())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Property(nameof(IAuditableEntity.CreatedAt)).CurrentValue = utcNow;
                    entry.Property(nameof(IAuditableEntity.CreatedBy)).CurrentValue = userId;
                }

                entry.Property(nameof(IAuditableEntity.ModifiedAt)).CurrentValue = utcNow;
                entry.Property(nameof(IAuditableEntity.ModifiedBy)).CurrentValue = userId;
            }
        }
    }
}

internal static class EntityEntryExtensions
{
    public static bool HasChangedOwnedEntities(this EntityEntry entry) =>
        entry.References.Any(r =>
            r.TargetEntry is not null &&
            r.TargetEntry.Metadata.IsOwned() &&
            r.TargetEntry.State is EntityState.Added or EntityState.Modified);
}
