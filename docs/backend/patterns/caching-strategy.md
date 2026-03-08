# Caching Strategy

**Created:** 2026-03-08
**Stack:** FusionCache (L1 in-memory) + IMemoryCache (authorization)

---

## Overview

NOIR uses **FusionCache** as the primary caching layer, providing stampede protection, fail-safe (serve stale data), and soft/hard timeouts out of the box. Authorization permissions use a separate **IMemoryCache** for tighter lifecycle control.

All cache keys are centralized in `CacheKeys` (no magic strings). Invalidation is explicit via domain-specific invalidator services.

---

## Architecture

```
Request
+-- Per-Request Dictionary (FeatureChecker)
|   +-- Avoids repeated FusionCache lookups within one HTTP request
+-- IMemoryCache (PermissionAuthorizationHandler)
|   +-- Direct TryGetValue/Set for authorization hot path
+-- FusionCache (L1 In-Memory)
    +-- Stampede protection (single factory execution)
    +-- Fail-safe (return stale data on backend failure)
    +-- Soft/hard factory timeouts
    +-- [Optional] Redis L2 + Backplane (multi-replica)
```

---

## Configuration

Settings in `appsettings.json` under `Cache` section, bound to `CacheSettings`:

| Setting | Default | Purpose |
|---------|---------|---------|
| `DefaultExpirationMinutes` | 30 | Default TTL for all entries |
| `PermissionExpirationMinutes` | 60 | Permissions (less volatile) |
| `UserProfileExpirationMinutes` | 15 | User profiles |
| `BlogPostExpirationMinutes` | 5 | Blog content (frequently updated) |
| `FailSafeMaxDurationMinutes` | 120 | Max stale data age on backend failure |
| `FactorySoftTimeoutMs` | 100 | Return stale if factory exceeds this |
| `FactoryHardTimeoutMs` | 2000 | Absolute max factory wait |
| `RedisConnectionString` | null | Enable L2 distributed cache |
| `EnableBackplane` | false | Cross-replica invalidation |

Registration: `services.AddFusionCaching(configuration)` in `Infrastructure/Caching/FusionCacheRegistration.cs`.

---

## Cache Key Conventions

All keys defined in `Infrastructure/Caching/CacheKeys.cs`. Pattern: `{prefix}:{qualifier}:{identifier}`.

| Prefix | Example Key | TTL |
|--------|------------|-----|
| `perm` | `perm:user:{userId}` | 60 min |
| `user` | `user:profile:{userId}`, `user:email:{email}` | 15 min |
| `role` | `role:id:{roleId}`, `role:all` | 30 min |
| `tenant` | `tenant:id:{tenantId}` | 30 min |
| `settings` | `settings:tenant:{tenantId}` | 30 min |
| `blog` | `blog:post:slug:{slug}`, `blog:posts:p1:s10` | 5 min |
| `email_template` | `email_template:{name}:{tenantId or platform}` | 30 min |
| `smtp_settings` | `smtp_settings:{tenantId or platform}` | 15 min |
| `features` | `features:tenant:{tenantId or platform}` | 5 min |
| `exchange_rate` | `exchange_rate:{from}:{to}` | configurable |

**Rules:**
- Always use `CacheKeys.*` static methods -- never inline key strings
- Tenant-scoped keys embed `tenantId` or `"platform"` for system-level entries
- Paginated lists include page/size in key: `blog:posts:p{page}:s{size}:c{category}`

---

## Caching Patterns

### Pattern 1: FusionCache GetOrSet (Most Common)

Used by: `FeatureChecker`, `EmailService`, `CurrencyService`, `TenantSettingsService`.

```csharp
var result = await _cache.GetOrSetAsync(
    CacheKeys.TenantFeatures(tenantId),
    async token => await LoadFromDbAsync(tenantId, token),
    options => options
        .SetDuration(TimeSpan.FromMinutes(5))
        .SetFailSafe(true, TimeSpan.FromHours(1)),
    ct);
```

FusionCache handles: concurrent callers (only one factory runs), stale fallback on DB failure, timeout protection.

### Pattern 2: Per-Request Dictionary Cache

Used by: `FeatureChecker` -- avoids repeated FusionCache lookups within a single HTTP request.

```csharp
public sealed class FeatureChecker : IFeatureChecker, IScopedService
{
    private IReadOnlyDictionary<string, EffectiveFeatureState>? _requestCache;

    public async Task<bool> IsEnabledAsync(string featureName, CancellationToken ct)
    {
        var states = await LoadStatesAsync(ct);  // Populates _requestCache on first call
        return states.TryGetValue(featureName, out var state) && state.IsEffective;
    }
}
```

The scoped lifetime ensures the dictionary lives for one request, then is discarded. FusionCache (cross-request) backs the dictionary.

### Pattern 3: IMemoryCache (Authorization)

Used by: `PermissionAuthorizationHandler` -- separate from FusionCache for direct `TryGetValue`/`Set` control.

```csharp
if (_cache.TryGetValue(cacheKey, out HashSet<string>? permissions))
    return permissions;

// ... load from UserManager/RoleManager ...
_cache.Set(cacheKey, permissions, cacheOptions);
PermissionCacheInvalidator.RegisterCachedUser(userId);
```

A static `HashSet<string>` tracks cached user IDs for bulk invalidation via `PermissionCacheInvalidator`.

---

## Cache Invalidation

### Invalidation Services

| Service | Interface | Scope |
|---------|-----------|-------|
| `CacheInvalidationService` | `ICacheInvalidationService` | User, role, blog, tenant settings, email templates |
| `PermissionCacheInvalidator` | `IPermissionCacheInvalidator` | Authorization permissions (IMemoryCache) |
| `FeatureCacheInvalidator` | `IFeatureCacheInvalidator` | Feature management states |

### Invalidation Triggers

| Event | Invalidation Call | Handler |
|-------|-------------------|---------|
| Role permission changed | `InvalidateRoleCacheAsync` + `InvalidateUser` | `AssignPermissionToRoleCommandHandler` |
| User roles changed | `InvalidateUserPermissionsAsync` + `InvalidateRoleAsync` | `AssignRolesToUserCommandHandler` |
| Blog post updated | `InvalidatePostCacheAsync(postId, slug)` | Post mutation handlers |
| Email template updated | `InvalidateEmailTemplateCacheAsync(name, tenantId)` | `UpdateEmailTemplateCommandHandler` |
| Module toggled | `FeatureCacheInvalidator.InvalidateAsync(tenantId)` | `ToggleModuleCommandHandler` |
| SMTP settings changed | `RemoveAsync(SmtpSettings key)` | `TenantSettingsService.SetSettingAsync` |
| Tenant provisioned/deleted | `InvalidateTenantSettingsAsync` | Tenant mutation handlers |

**Pattern:** Invalidation happens in the command handler, immediately after `SaveChangesAsync`.

---

## Multi-Tenant Cache Isolation

Cache keys embed the tenant identifier to prevent cross-tenant data leakage:

```
Tenant-scoped:
  features:tenant:abc123
  settings:tenant:abc123
  email_template:welcome:abc123

Platform-level (null tenantId):
  features:tenant:platform
  smtp_settings:platform
```

Invalidation is always tenant-scoped -- toggling a module for tenant A does not affect tenant B cache.

---

## When to Cache

**Cache:**
- Data read far more often than written (permissions, feature flags, settings)
- Expensive queries (recursive CTEs, multi-join aggregations)
- External API responses (exchange rates, SMTP discovery)
- Data that tolerates brief staleness (blog posts, RSS feeds)

**Do NOT cache:**
- Transactional data (orders, payments, inventory counts)
- User-specific mutable state (cart contents, form drafts)
- Data that must be real-time accurate (stock levels, OTP tokens)
- Data already behind SignalR real-time updates

---

## Adding New Cache Entries

1. **Add cache key** to `Infrastructure/Caching/CacheKeys.cs`:
   ```csharp
   public static string MyEntity(Guid id) => $"myprefix:id:{id}";
   ```

2. **Use GetOrSetAsync** in the service:
   ```csharp
   return await _cache.GetOrSetAsync(
       CacheKeys.MyEntity(id),
       async token => await LoadFromDbAsync(id, token),
       options => options.SetDuration(TimeSpan.FromMinutes(15)),
       ct);
   ```

3. **Add invalidation** to `ICacheInvalidationService` and `CacheInvalidationService`:
   ```csharp
   public async Task InvalidateMyEntityCacheAsync(Guid id, CancellationToken ct)
   {
       await _cache.RemoveAsync(CacheKeys.MyEntity(id), token: ct);
   }
   ```

4. **Call invalidation** in the mutation command handler after `SaveChangesAsync`.

5. **For multi-tenant data**, always include tenantId in the cache key.

6. **For high-frequency reads** (middleware, authorization), consider the per-request dictionary pattern on top of FusionCache (see `FeatureChecker`).

---

## File Reference

| File | Purpose |
|------|---------|
| `Application/Common/Settings/CacheSettings.cs` | Configuration POCO |
| `Infrastructure/Caching/CacheKeys.cs` | All cache key definitions |
| `Infrastructure/Caching/FusionCacheRegistration.cs` | DI registration |
| `Infrastructure/Caching/CacheInvalidationService.cs` | General invalidation |
| `Infrastructure/Services/FeatureChecker.cs` | Per-request + FusionCache pattern |
| `Infrastructure/Services/FeatureCacheInvalidator.cs` | Feature flag invalidation |
| `Infrastructure/Identity/Authorization/PermissionCacheInvalidator.cs` | IMemoryCache invalidation |
| `Infrastructure/Identity/Authorization/PermissionAuthorizationHandler.cs` | IMemoryCache usage |