using NOIR.Infrastructure.Audit;

namespace NOIR.Infrastructure.Persistence.Interceptors;

/// <summary>
/// EF Core interceptor that creates EntityAuditLog entries for all entity changes.
/// Bottom level of the hierarchical audit logging system.
/// Captures entity-level changes with RFC 6902 JSON Patch diff (extended with oldValue).
/// </summary>
/// <remarks>
/// Respects the following attributes:
/// - [DisableAuditing] on entity class: Skips auditing for the entire entity
/// - [DisableAuditing] on property: Skips auditing for that property
/// - [AuditSensitive] on property: Redacts the value in the audit diff
/// </remarks>
public class EntityAuditLogInterceptor : SaveChangesInterceptor
{
    private readonly IDiffService _diffService;
    private readonly IMultiTenantContextAccessor<TenantInfo> _tenantContextAccessor;

    // Cache for entity type audit settings
    private static readonly ConcurrentDictionary<Type, bool> EntityAuditDisabledCache = new();
    private static readonly ConcurrentDictionary<string, PropertyAuditInfo> PropertyAuditInfoCache = new();

    // Properties to exclude from audit logging (sensitive data) - used as fallback
    private static readonly HashSet<string> SensitiveProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "Password", "PasswordHash", "SecurityStamp", "ConcurrencyStamp",
        "Secret", "Token", "ApiKey", "PrivateKey", "Salt", "RefreshToken",
        "CreditCard", "CVV", "SSN", "SocialSecurityNumber"
    };

    // Entity types to exclude from audit logging (internal system entities)
    private static readonly HashSet<string> ExcludedEntityTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(HttpRequestAuditLog),
        nameof(HandlerAuditLog),
        nameof(EntityAuditLog),
        "IdentityUserClaim`1",
        "IdentityUserRole`1",
        "IdentityUserLogin`1",
        "IdentityUserToken`1"
    };

    public EntityAuditLogInterceptor(
        IDiffService diffService,
        IMultiTenantContextAccessor<TenantInfo> tenantContextAccessor)
    {
        _diffService = diffService;
        _tenantContextAccessor = tenantContextAccessor;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context is null) return await base.SavingChangesAsync(eventData, result, cancellationToken);

        var auditEntries = CreateAuditEntries(context);

        // Add audit logs to the same transaction
        foreach (var entry in auditEntries)
        {
            context.Set<EntityAuditLog>().Add(entry);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private List<EntityAuditLog> CreateAuditEntries(DbContext context)
    {
        context.ChangeTracker.DetectChanges();
        var auditEntries = new List<EntityAuditLog>();

        // IMPORTANT: Capture AuditContext.Current once and extract values immediately
        // to avoid TOCTOU race conditions. The context is AsyncLocal and could change
        // between reads if not captured as a snapshot.
        var auditContext = AuditContext.Current;
        var correlationId = auditContext?.CorrelationId ?? Guid.NewGuid().ToString();
        var handlerAuditLogId = auditContext?.CurrentHandlerAuditLogId;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            var entityType = entry.Entity.GetType();
            var entityTypeName = entityType.Name;

            // Skip excluded entity types to prevent recursion and noise
            if (ExcludedEntityTypes.Contains(entityTypeName) ||
                ExcludedEntityTypes.Any(e => entityTypeName.Contains(e)))
                continue;

            // Skip entities with [DisableAuditing] attribute
            if (IsEntityAuditingDisabled(entityType))
                continue;

            // Skip unchanged and detached entities
            if (entry.State is EntityState.Unchanged or EntityState.Detached)
                continue;

            var entityId = GetPrimaryKey(entry);
            var operation = MapStateToOperation(entry.State);

            // Build before/after dictionaries
            var beforeValues = new Dictionary<string, object?>();
            var afterValues = new Dictionary<string, object?>();

            foreach (var property in entry.Properties)
            {
                var propertyName = property.Metadata.Name;
                var propertyInfo = GetPropertyAuditInfo(entityType, propertyName);

                // Skip properties with [DisableAuditing] attribute
                if (propertyInfo.IsDisabled)
                    continue;

                // Skip sensitive properties (by attribute or by name convention)
                if (propertyInfo.IsSensitive || IsSensitivePropertyByName(propertyName))
                    continue;

                // Skip primary key from values (we store it separately)
                if (property.Metadata.IsPrimaryKey())
                    continue;

                switch (entry.State)
                {
                    case EntityState.Added:
                        afterValues[propertyName] = property.CurrentValue;
                        break;

                    case EntityState.Deleted:
                        beforeValues[propertyName] = property.OriginalValue;
                        break;

                    case EntityState.Modified:
                        if (property.IsModified && !Equals(property.OriginalValue, property.CurrentValue))
                        {
                            beforeValues[propertyName] = property.OriginalValue;
                            afterValues[propertyName] = property.CurrentValue;
                        }
                        break;
                }
            }

            // Only create audit entry if there are actual changes
            if (entry.State == EntityState.Modified && beforeValues.Count == 0)
                continue;

            // Create the diff using the diff service
            var entityDiff = _diffService.CreateDiffFromDictionaries(
                beforeValues.Count > 0 ? beforeValues : null,
                afterValues.Count > 0 ? afterValues : null);

            // Get tenant ID
            string? tenantId = _tenantContextAccessor.MultiTenantContext?.TenantInfo?.Id;
            if (string.IsNullOrEmpty(tenantId) && entry.Entity is ITenantEntity tenantEntity)
            {
                tenantId = tenantEntity.TenantId;
            }

            var auditLog = EntityAuditLog.Create(
                correlationId: correlationId,
                entityType: entityTypeName,
                entityId: entityId ?? "unknown",
                operation: operation,
                entityDiff: entityDiff,
                tenantId: tenantId,
                handlerAuditLogId: handlerAuditLogId);

            auditEntries.Add(auditLog);
        }

        // Track navigation property (collection) changes
        var collectionAuditEntries = CreateCollectionAuditEntries(context, correlationId, handlerAuditLogId);
        auditEntries.AddRange(collectionAuditEntries);

        return auditEntries;
    }

    /// <summary>
    /// Creates audit entries for collection navigation property changes.
    /// Tracks items added to or removed from collections marked with [AuditCollection].
    /// </summary>
    private List<EntityAuditLog> CreateCollectionAuditEntries(
        DbContext context,
        string correlationId,
        Guid? handlerAuditLogId)
    {
        var auditEntries = new List<EntityAuditLog>();

        // Look for entities with navigation properties that have the [AuditCollection] attribute
        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is EntityState.Unchanged or EntityState.Detached)
                continue;

            var entityType = entry.Entity.GetType();

            // Check each collection navigation property
            foreach (var navigation in entry.Navigations.OfType<CollectionEntry>())
            {
                var navProperty = entityType.GetProperty(navigation.Metadata.Name);
                if (navProperty is null)
                    continue;

                var auditCollectionAttr = navProperty.GetCustomAttribute<AuditCollectionAttribute>();
                if (auditCollectionAttr is null)
                    continue;

                // Track changes to this collection
                var collectionChanges = GetCollectionChanges(navigation, auditCollectionAttr);
                if (collectionChanges is null || (collectionChanges.Added.Count == 0 && collectionChanges.Removed.Count == 0))
                    continue;

                // Get parent entity info
                var parentEntityId = GetPrimaryKey(entry);
                string? tenantId = _tenantContextAccessor.MultiTenantContext?.TenantInfo?.Id;
                if (string.IsNullOrEmpty(tenantId) && entry.Entity is ITenantEntity tenantEntity)
                {
                    tenantId = tenantEntity.TenantId;
                }

                // Create diff for collection changes
                var diff = CreateCollectionDiff(navigation.Metadata.Name, collectionChanges);

                if (!string.IsNullOrEmpty(diff))
                {
                    var auditLog = EntityAuditLog.Create(
                        correlationId: correlationId,
                        entityType: entityType.Name,
                        entityId: parentEntityId ?? "unknown",
                        operation: EntityAuditOperation.Modified,
                        entityDiff: diff,
                        tenantId: tenantId,
                        handlerAuditLogId: handlerAuditLogId);

                    auditEntries.Add(auditLog);
                }
            }
        }

        return auditEntries;
    }

    /// <summary>
    /// Gets the added and removed items from a collection navigation property.
    /// Fixed: Added proper null checks and parent-child relationship verification.
    /// </summary>
    private CollectionChanges? GetCollectionChanges(CollectionEntry navigation, AuditCollectionAttribute attr)
    {
        // Defensive null checks
        if (navigation?.EntityEntry?.Context is null)
            return null;

        if (!navigation.IsLoaded)
            return null;

        var targetEntityType = navigation.Metadata.TargetEntityType?.ClrType;
        if (targetEntityType is null)
            return null;

        var added = new List<string>();
        var removed = new List<string>();

        // Get the parent entity info for relationship verification
        var parentEntry = navigation.EntityEntry;
        var parentKey = GetPrimaryKey(parentEntry);

        // Check if any tracked entries relate to this collection
        var context = navigation.EntityEntry.Context;
        var collectionEntries = context.ChangeTracker.Entries()
            .Where(e => e.Metadata.ClrType == targetEntityType)
            .Where(e => e.State is EntityState.Added or EntityState.Deleted)
            .ToList();

        // Get the foreign key property name to verify parent-child relationship
        // CollectionEntry.Metadata is INavigationBase which doesn't have ForeignKey directly
        // Need to cast to INavigation for skip navigations or get from the target entity type
        var fkPropertyName = (navigation.Metadata as Microsoft.EntityFrameworkCore.Metadata.INavigation)?
            .ForeignKey?.Properties.FirstOrDefault()?.Name;

        foreach (var childEntry in collectionEntries)
        {
            // Verify this child belongs to the parent (if we can determine the FK)
            if (!string.IsNullOrEmpty(fkPropertyName) && !string.IsNullOrEmpty(parentKey))
            {
                var fkProperty = childEntry.Properties.FirstOrDefault(p => p.Metadata.Name == fkPropertyName);
                var fkValue = fkProperty?.CurrentValue?.ToString() ?? fkProperty?.OriginalValue?.ToString();

                // Skip if this child doesn't belong to this parent
                // Use OrdinalIgnoreCase for GUID/string PK comparison
                if (fkValue is not null && !string.Equals(fkValue, parentKey, StringComparison.OrdinalIgnoreCase))
                    continue;
            }

            var childId = GetPrimaryKey(childEntry);
            var displayValue = GetDisplayValue(childEntry, attr);
            var displayText = displayValue ?? childId ?? (childEntry.State == EntityState.Added ? "new" : "deleted");

            if (childEntry.State == EntityState.Added)
            {
                added.Add(displayText);
            }
            else if (childEntry.State == EntityState.Deleted)
            {
                removed.Add(displayText);
            }
        }

        return new CollectionChanges(added, removed);
    }

    /// <summary>
    /// Gets the display value for a child entity based on the AuditCollection attribute configuration.
    /// </summary>
    private static string? GetDisplayValue(EntityEntry? entry, AuditCollectionAttribute attr)
    {
        if (entry is null) return null;

        if (string.IsNullOrEmpty(attr.ChildDisplayProperty))
            return GetPrimaryKey(entry);

        var displayProperty = entry.Properties?.FirstOrDefault(p => p.Metadata.Name == attr.ChildDisplayProperty);
        return displayProperty?.CurrentValue?.ToString();
    }

    /// <summary>
    /// Creates an RFC 6902-like diff for collection changes.
    /// </summary>
    private string CreateCollectionDiff(string propertyName, CollectionChanges changes)
    {
        var operations = new List<object>();

        foreach (var added in changes.Added)
        {
            operations.Add(new
            {
                op = "add",
                path = $"/{propertyName}/-",
                value = added
            });
        }

        foreach (var removed in changes.Removed)
        {
            operations.Add(new
            {
                op = "remove",
                path = $"/{propertyName}",
                oldValue = removed
            });
        }

        if (operations.Count == 0)
            return string.Empty;

        return System.Text.Json.JsonSerializer.Serialize(operations, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });
    }

    private sealed record CollectionChanges(List<string> Added, List<string> Removed);

    private static EntityAuditOperation MapStateToOperation(EntityState state)
    {
        return state switch
        {
            EntityState.Added => EntityAuditOperation.Added,
            EntityState.Modified => EntityAuditOperation.Modified,
            EntityState.Deleted => EntityAuditOperation.Deleted,
            _ => EntityAuditOperation.Modified
        };
    }

    private static string? GetPrimaryKey(EntityEntry? entry)
    {
        if (entry is null) return null;
        var keyProperty = entry.Properties?.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
        return keyProperty?.CurrentValue?.ToString();
    }

    /// <summary>
    /// Checks if an entity type has auditing disabled via [DisableAuditing] attribute.
    /// Results are cached for performance.
    /// </summary>
    private static bool IsEntityAuditingDisabled(Type entityType)
    {
        return EntityAuditDisabledCache.GetOrAdd(entityType, type =>
            type.GetCustomAttribute<DisableAuditingAttribute>() is not null);
    }

    /// <summary>
    /// Gets audit information for a property via attributes.
    /// Results are cached for performance.
    /// </summary>
    private static PropertyAuditInfo GetPropertyAuditInfo(Type entityType, string propertyName)
    {
        var cacheKey = $"{entityType.FullName}.{propertyName}";
        return PropertyAuditInfoCache.GetOrAdd(cacheKey, _ =>
        {
            var property = entityType.GetProperty(propertyName);
            if (property is null)
            {
                return new PropertyAuditInfo(false, false, null);
            }

            var isDisabled = property.GetCustomAttribute<DisableAuditingAttribute>() is not null;
            var sensitiveAttr = property.GetCustomAttribute<AuditSensitiveAttribute>();
            var isSensitive = sensitiveAttr is not null;
            var sensitivityMask = sensitiveAttr?.Mask;

            return new PropertyAuditInfo(isDisabled, isSensitive, sensitivityMask);
        });
    }

    /// <summary>
    /// Checks if a property is sensitive by naming convention (fallback).
    /// </summary>
    private static bool IsSensitivePropertyByName(string propertyName)
    {
        return SensitiveProperties.Any(s =>
            propertyName.Contains(s, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Cached property audit information.
    /// </summary>
    private sealed record PropertyAuditInfo(bool IsDisabled, bool IsSensitive, string? SensitivityMask);
}
