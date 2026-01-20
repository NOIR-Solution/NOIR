# TenantIdSetterInterceptor - Multi-Tenant Entity Management

## Overview

The `TenantIdSetterInterceptor` is an EF Core SaveChanges interceptor that automatically sets the `TenantId` property on entities implementing `ITenantEntity` before they are saved to the database. This ensures proper data isolation in the multi-tenant architecture.

## How It Works

```csharp
public class TenantIdSetterInterceptor : SaveChangesInterceptor
{
    private readonly IMultiTenantContextAccessor<Tenant> _tenantContextAccessor;

    private void SetTenantId(DbContext? context)
    {
        if (context == null) return;

        var tenantId = _tenantContextAccessor.MultiTenantContext?.TenantInfo?.Id;
        if (string.IsNullOrEmpty(tenantId)) return;

        foreach (var entry in context.ChangeTracker.Entries<ITenantEntity>())
        {
            // CRITICAL: Never modify TenantId for system users - they must remain tenant-agnostic
            if (entry.Entity is ApplicationUser user && user.IsSystemUser)
                continue;

            if (entry.State == EntityState.Added && string.IsNullOrEmpty(entry.Entity.TenantId))
            {
                entry.Property(nameof(ITenantEntity.TenantId)).CurrentValue = tenantId;
            }
        }
    }
}
```

### Key Behaviors

1. **Automatic Tenant Assignment**: When a new entity is added (`EntityState.Added`) and has no `TenantId`, the interceptor sets it to the current tenant from the `IMultiTenantContextAccessor`

2. **System User Protection**: System users (like platform admin with `IsSystemUser = true`) are **never** modified by the interceptor, regardless of entity state. This ensures platform admins remain tenant-agnostic with `TenantId = null`.

3. **Update Protection**: The interceptor only acts on `EntityState.Added` (INSERT operations), not updates. This prevents entities from accidentally moving between tenants.

## Critical Fix: System User Protection

### The Problem (Before Fix)

Previously, the system user check was **inside** the `EntityState.Added` condition:

```csharp
// ❌ WRONG: System users could be modified during updates
if (entry.State == EntityState.Added && string.IsNullOrEmpty(entry.Entity.TenantId))
{
    if (entry.Entity is ApplicationUser user && user.IsSystemUser)
        continue;

    entry.Property(nameof(ITenantEntity.TenantId)).CurrentValue = tenantId;
}
```

This caused issues during database seeding:
1. Platform admin created with `TenantId = null` ✅
2. Default tenant created and set as current context
3. Seeder runs `userManager.UpdateAsync(platformAdmin)` to update password/roles
4. Interceptor sees update operation → skips entire block → **doesn't protect system user**
5. If any other code path triggered a save with system user in change tracker, it could get modified

### The Solution (Current)

Move system user check **before** entity state check:

```csharp
// ✅ CORRECT: System users are protected in ALL scenarios
if (entry.Entity is ApplicationUser user && user.IsSystemUser)
    continue;

if (entry.State == EntityState.Added && string.IsNullOrEmpty(entry.Entity.TenantId))
{
    entry.Property(nameof(ITenantEntity.TenantId)).CurrentValue = tenantId;
}
```

Now system users are **completely ignored** by the interceptor, regardless of:
- Entity state (Added, Modified, etc.)
- Current tenant context
- Any database operation

## Verification

### During Application Startup

Check the logs for platform admin creation:

```
[INF] Created platform admin user: platform@noir.local (TenantId = null)
```

### In Database

Query to verify platform admin has no tenant:

```sql
SELECT Email, TenantId, IsSystemUser
FROM AspNetUsers
WHERE Email = 'platform@noir.local'

-- Expected Result:
-- Email: platform@noir.local
-- TenantId: NULL
-- IsSystemUser: 1
```

### In Dashboard

After logging in as platform admin:
- Email: `platform@noir.local`
- **Tenant: Platform** (not a specific tenant ID)
- Roles: Platform Admin

## Related Components

| Component | Purpose |
|-----------|---------|
| `ITenantEntity` | Interface marking entities that belong to a tenant |
| `ApplicationUser` | User entity with `IsSystemUser` flag for platform admins |
| `IMultiTenantContextAccessor<Tenant>` | Provides current tenant context |
| `ApplicationDbContextSeeder` | Seeds platform admin with `TenantId = null` and `IsSystemUser = true` |

## Best Practices

1. **System Users**: Always set `IsSystemUser = true` for cross-tenant users (platform admins, system processes)
2. **Testing**: Verify system users maintain `TenantId = null` after any user management operations
3. **Seeding**: Create platform admin **before** setting tenant context in seeder
4. **Migration**: Never manually set `TenantId` in migrations - let the interceptor handle it

## Troubleshooting

### Platform Admin Shows Tenant ID

**Symptom**: Dashboard shows specific tenant ID instead of "Platform"

**Cause**: Platform admin user has non-null `TenantId` in database

**Solution**:
1. Check `IsSystemUser` flag: `SELECT IsSystemUser FROM AspNetUsers WHERE Email = 'platform@noir.local'`
2. If `IsSystemUser = false`, the seeder's update logic will fix it on next startup
3. Manual fix: `UPDATE AspNetUsers SET TenantId = NULL, IsSystemUser = 1 WHERE Email = 'platform@noir.local'`
4. Restart application and re-login

### New Entities Not Getting TenantId

**Symptom**: `TenantId` remains null for new entities

**Possible Causes**:
1. No tenant context set (`IMultiTenantContextAccessor` is null)
2. Entity doesn't implement `ITenantEntity`
3. `TenantId` was explicitly set before save (interceptor skips already-set values)

**Solution**: Verify tenant resolution middleware is running and entity implements `ITenantEntity`

## References

- **Interceptor**: `src/NOIR.Infrastructure/Persistence/Interceptors/TenantIdSetterInterceptor.cs`
- **Interface**: `src/NOIR.Domain/Common/ITenantEntity.cs`
- **Seeder**: `src/NOIR.Infrastructure/Persistence/ApplicationDbContextSeeder.cs`
- **User Entity**: `src/NOIR.Infrastructure/Identity/ApplicationUser.cs`
