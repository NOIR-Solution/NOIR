# NOIR Knowledge Base

**Last Updated:** 2026-02-18
**Version:** 3.0

A comprehensive cross-referenced guide to the NOIR codebase, patterns, and architecture.

---

## Quick Navigation

| Section | Description |
|---------|-------------|
| [Recent Fixes & Improvements](#recent-fixes--improvements) | Latest bug fixes and architectural changes |
| [Architecture Overview](#architecture-overview) | Clean Architecture layers and dependencies |
| [Domain Layer](#domain-layer) | Entities, interfaces, value objects |
| [Application Layer](#application-layer) | Features, commands, queries, specifications |
| [Infrastructure Layer](#infrastructure-layer) | Persistence, identity, services |
| [Web Layer](#web-layer) | API endpoints, middleware, frontend |
| [Cross-Cutting Concerns](#cross-cutting-concerns) | Audit, auth, validation, multi-tenancy |
| [Development Guide](#development-guide) | Common tasks and patterns |
| [Testing](#testing) | Test structure and patterns |
| [Documentation Map](#documentation-map) | All docs with descriptions |

---

## Recent Fixes & Improvements

**Last Session:** 2026-02-01

### Documentation Audit & Restructure (2026-02-01)

**Scope:** Comprehensive documentation review and cleanup for 100% quality score.

**Changes Applied:**

1. **Duplicate Research Documents Consolidated**
   - Merged `role-permission-best-practices-2025.md` and `role-permission-management-research.md`
   - Created single comprehensive `role-permission-system-research.md`
   - Combines industry analysis (RBAC vs ReBAC vs ABAC) with practical enhancement recommendations
   - Presents two implementation paths: Quick Enhancement vs Full Evolution
   - Deleted the two duplicate files

2. **DOCUMENTATION_INDEX.md Updates**
   - Added missing `vietnam-shipping-integration-2026.md` to backend research section
   - Updated to reference consolidated `role-permission-system-research.md`
   - Updated statistics: 44 total docs (7 backend research files)
   - Version bumped to 2.2

3. **README.md Restructure**
   - Complete rewrite with accurate folder structure
   - Added all 44 files to structure tree
   - Added Research and Architecture Decisions quick links

4. **All References Updated**
   - Updated docs/README.md, docs/backend/README.md, PROJECT_INDEX.md
   - All links now point to consolidated `role-permission-system-research.md`

**Files Modified:**
- `docs/DOCUMENTATION_INDEX.md`
- `docs/README.md`
- `docs/KNOWLEDGE_BASE.md`
- `docs/FEATURE_CATALOG.md`
- `docs/PROJECT_INDEX.md`
- `docs/backend/README.md`

**Files Created:**
- `docs/backend/research/role-permission-system-research.md` (consolidated)

**Files Deleted:**
- `docs/backend/research/role-permission-best-practices-2025.md`
- `docs/backend/research/role-permission-management-research.md`

---

### UI/UX Standardization (2026-01-26)

**Scope:** Portal-wide consistency audit and fixes across e-commerce and admin sections.

**Changes Applied:**

1. **AlertDialog Pattern Standardization**
   - Added `border-destructive/30` to all destructive AlertDialogContent
   - Changed icon containers from various styles to `p-2 rounded-xl bg-destructive/10 border border-destructive/20`
   - Added `cursor-pointer` to all Cancel and Action buttons

2. **Accessibility Improvements**
   - Added `aria-label` to all icon-only buttons (View, Edit, Delete, Back navigation)
   - Labels are contextual: `aria-label={`View ${product.name} details`}`

3. **Confirmation Dialogs**
   - Added confirmation dialogs for variant deletion in ProductFormPage
   - Added confirmation dialogs for image deletion in ProductFormPage
   - Pattern: State variable for item-to-delete, separate dialog component

4. **Visual Consistency**
   - Standardized card shadows: `shadow-sm hover:shadow-lg transition-all duration-300`
   - Fixed gradient text requiring `text-transparent` class with `bg-clip-text`

**Files Modified:**
- E-commerce: `ProductFormPage.tsx`, `ProductsPage.tsx`, `ProductCategoriesPage.tsx`, `EnhancedProductCard.tsx`, `DeleteProductDialog.tsx`
- Admin: `DeleteRoleDialog.tsx`
- Blog: `DeletePostDialog.tsx`, `DeleteCategoryDialog.tsx`, `DeleteTagDialog.tsx`
- Settings: `SessionManagement.tsx`

**Documentation:** See [Frontend Architecture - UI/UX Standardization Patterns](frontend/architecture.md#uiux-standardization-patterns)

---

### EF Core Migration Tooling Workaround (2026-01-25)

**Issue:** EF Core migration tools had a Roslyn compatibility issue (`ReflectionTypeLoadException` with `Microsoft.CodeAnalysis.VisualBasic.Workspaces`) that prevented:
1. Creating new migrations via `dotnet ef migrations add`
2. Application startup (failed with `PendingModelChangesWarning`)

**Temporary Solution:** Suppressed `PendingModelChangesWarning` in DEBUG mode only:
- Location: `src/NOIR.Infrastructure/DependencyInjection.cs:83-84`
- Pattern already exists in test factories (`CustomWebApplicationFactory.cs`, `LocalDbWebApplicationFactory.cs`)
- Allows development to continue while tooling issue is resolved

**Follow-up Actions:**
- Monitor EF Core/Roslyn package updates
- Remove workaround once tooling is fixed
- Test migration creation after each EF Core version update

---

### Finbuckle.MultiTenant 10.0.2 Breaking Change Migration

**Issue:** Upgrading Finbuckle.MultiTenant from 9.x to 10.0.2 introduced a breaking change where `TenantInfo` changed from a `record` to a `class` with `required init` properties.

**Breaking Changes:**
- `TenantInfo.Id`, `Identifier` are now `required init`-only → cannot be mutated after construction
- `TenantInfo` is a `class`, not a `record` → `with` expressions no longer work
- `IMultiTenantDbContext.TenantInfo` returns `ITenantInfo?` instead of `TenantInfo?`

**Solution:** Converted `Tenant` entity from `record` to `class` with immutable factory methods:

```csharp
public class Tenant : TenantInfo, IAuditableEntity
{
    [SetsRequiredMembers]
    private Tenant() { Id = string.Empty; Identifier = string.Empty; }

    [SetsRequiredMembers]
    public Tenant(string id, string identifier, string? name = null) { ... }

    // Immutable factory methods (return new instances)
    public static Tenant Create(string identifier, string name, ...) { ... }
    public Tenant CreateUpdated(string identifier, string name, ...) { ... }
    public Tenant CreateActivated() { ... }
    public Tenant CreateDeactivated() { ... }
    public Tenant CreateDeleted(string? deletedBy = null) { ... }
}
```

**Key Patterns:**
- `[SetsRequiredMembers]` attribute satisfies C# `required` member constraints
- Factory methods create new instances instead of mutating (init-only properties)
- EF Core tracked entities use `Entry.CurrentValues.SetValues()` for updates
- Finbuckle's `IMultiTenantStore.UpdateAsync()` handles detached entity replacement

**Files Modified:**
- `src/NOIR.Domain/Entities/Tenant.cs` - Core entity rewrite
- `src/NOIR.Application/Features/Tenants/Commands/*/` - Handler updates
- `src/NOIR.Infrastructure/Persistence/ApplicationDbContext.cs` - `ITenantInfo?` return type
- `src/NOIR.Infrastructure/Persistence/Seeders/TenantSeeder.cs` - Direct mutation for tracked entities

---

### Activity Timeline Tenant Filtering Fix

**Issue:** Tenant admins could see platform-level activities in the Activity Timeline by clicking on activity entries.

**Root Cause:** `AuditLogQueryService.GetActivityDetailsAsync` wasn't checking tenant access before returning activity details.

**Fix:** Added tenant access check in `GetActivityDetailsAsync`:
```csharp
// Tenant access check: Non-platform admins can only view activities from their tenant
if (!_currentUser.IsPlatformAdmin && handler.TenantId != _currentUser.TenantId)
{
    return Result.Failure<ActivityDetailsDto>(
        Error.Forbidden("You do not have permission to view this activity.", ErrorCodes.Auth.Forbidden));
}
```

**API Change:** `GetActivityDetailsAsync` now returns `Result<ActivityDetailsDto>` instead of `ActivityDetailsDto?` to properly propagate Forbidden/NotFound errors.

---

### Wolverine Development Mode Fix (start-dev.sh)

**Issue:** After restarting the development server, all API calls failed with `JasperFx.CodeGeneration.ExpectedTypeMissingException`.

**Root Cause:** The `start-dev.sh` script used `--no-launch-profile` without setting `ASPNETCORE_ENVIRONMENT`, defaulting to Production mode where Wolverine uses `TypeLoadMode.Static` (expects pre-built handlers).

**Fix:** Updated `start-dev.sh` with two changes:

1. **Set environment explicitly:**
```bash
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS="http://localhost:$BACKEND_PORT" dotnet run ...
```

2. **Clean generated handlers before build:**
```bash
# Clean Wolverine generated handlers (prevents stale handler errors)
GENERATED_DIR="$BACKEND_DIR/Internal/Generated"
if [[ -d "$GENERATED_DIR" ]]; then
    rm -rf "$GENERATED_DIR"
fi
```

**Key Insight:** Wolverine's `TypeLoadMode` configuration in `Program.cs`:
```csharp
opts.CodeGeneration.TypeLoadMode = builder.Environment.IsProduction()
    ? TypeLoadMode.Static   // Pre-built handlers (fast startup)
    : TypeLoadMode.Auto;    // Generate at runtime (development)
```

---

### Platform Admin Tenant ID Bug Fix

**Issue:** Platform admins were showing a tenant GUID in the dashboard instead of "Platform" (null tenant).

**Root Cause:** `GetCurrentUserQueryHandler` was using `_currentUser.TenantId` (HTTP context) instead of `user.TenantId` (database).

**Fix:** Changed line 53 in `GetCurrentUserQueryHandler.cs` to use database value:
```csharp
// ✅ CORRECT
var userDto = new CurrentUserDto(
    ...
    user.TenantId,  // Use TenantId from database, not HTTP context
    ...
);
```

**Key Insight:** Request context (`_currentUser`) is for **scoping/filtering**, database entity is for **user properties**.

**Verification:**
- Dashboard displays "Platform" not a GUID
- `/api/auth/me` response omits `tenantId` field (null value)
- JWT token has no `tenant_id` claim

---

### Removal of Tenant Fallback Strategy

**Issue:** Finbuckle middleware had `.WithStaticStrategy("default")` which set a fallback tenant when JWT had no `tenant_id` claim, causing inconsistency.

**Solution:** Removed static fallback strategy entirely for consistency:

```csharp
// BEFORE (inconsistent)
services.AddMultiTenant<Tenant>()
    .WithHeaderStrategy("X-Tenant")
    .WithClaimStrategy("tenant_id")
    .WithStaticStrategy("default")   // ❌ Fallback caused inconsistency
    .WithEFCoreStore<TenantStoreDbContext, Tenant>();

// AFTER (consistent)
services.AddMultiTenant<Tenant>()
    .WithHeaderStrategy("X-Tenant")
    .WithClaimStrategy("tenant_id")
    .WithEFCoreStore<TenantStoreDbContext, Tenant>();  // ✅ No fallback
```

**Impact:**
- Database seeder: Still works - explicitly sets tenant context via `IMultiTenantContextSetter`
- Background jobs: Still work - use EF Core global query filters, don't access `ICurrentUser`
- Platform admin: Now has `_currentUser.TenantId = NULL` consistently ✅

**Result:** When tenant is null, it's now null **everywhere** - no more confusion!

---

### Platform/Tenant Pattern Optimization

**Improvement:** Added `DatabaseConstants` for consistent schema sizing across platform/tenant entities.

**Constants:**
- `TenantIdMaxLength = 64` - Consistent tenant ID column size
- `UserIdMaxLength = 450` - ASP.NET Identity user ID max length

**Benefit:** Prevents migration inconsistencies and ensures optimal index performance.

**Filtered Indexes:** Platform default lookups are 2-3x faster with filtered indexes:
```sql
CREATE INDEX IX_EntityName_Platform_Lookup
ON EntityTable (Name, IsActive)
WHERE TenantId IS NULL AND IsDeleted = 0;
```

**Doc:** [backend/architecture/tenant-id-interceptor.md](backend/architecture/tenant-id-interceptor.md)

---

### CurrentUserLoaderMiddleware Pattern

**Improvement:** Centralized user profile loading in middleware (commit 8c411e6).

**Before:** Multiple endpoints and services were independently loading user data from database, causing:
- Repeated database queries per request
- Inconsistent user context across request pipeline
- Scattered user loading logic

**After:** Single middleware loads complete user profile once per request:

```csharp
public class CurrentUserLoaderMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _userRepository.GetByIdAsync(userId);
            context.Items["CurrentUserProfile"] = user;  // Cache for request
        }
        await _next(context);
    }
}
```

**Benefits:**
- **Performance:** One DB query per request instead of multiple
- **Consistency:** Same user data across entire request pipeline
- **Simplified code:** Services just read from `HttpContext.Items`

**Key Insight:** Middleware runs **after** multi-tenant resolution so it queries the correct tenant's database partition.

**Reference:** `src/NOIR.Web/Middleware/CurrentUserLoaderMiddleware.cs`

---

### Role Constants Centralization

**Improvement:** Unified role name constants in `RoleConstants` class (commit 8c411e6).

**Before:** Role names were magic strings scattered across 15+ files:
```csharp
if (user.Roles.Contains("PlatformAdmin"))  // ❌ Typo-prone
```

**After:** Centralized constants prevent typos and enable refactoring:
```csharp
public static class RoleConstants
{
    public const string PlatformAdmin = "PlatformAdmin";
    public const string TenantOwner = "TenantOwner";
    public const string TenantAdmin = "TenantAdmin";
    // ...
}

if (user.Roles.Contains(RoleConstants.PlatformAdmin))  // ✅ Type-safe
```

**Impact:** Refactored 15+ files to use constants, eliminating magic strings.

---

### Tooltip Migration to Radix UI

**Improvement:** Replaced custom tooltip with Radix UI `Tooltip` component (commit cc3d713).

**Before:** Custom CSS-based tooltip with accessibility issues and browser inconsistencies.

**After:** Radix UI primitives provide:
- ARIA-compliant accessibility
- Keyboard navigation support
- Portal rendering (avoids z-index issues)
- Consistent cross-browser behavior

**Reference:** `src/NOIR.Web/frontend/src/uikit/tooltip/Tooltip.tsx`

---

### RefreshToken Filtering Fix

**Bug Fix:** Fixed refresh token lookup to exclude expired tokens (commit eb8bd1d).

**Issue:** `GetActiveRefreshTokenAsync` was returning expired tokens, causing refresh failures.

**Solution:**
```csharp
// BEFORE (returned expired tokens)
var token = await _dbContext.RefreshTokens
    .FirstOrDefaultAsync(t => t.Token == refreshToken && t.IsActive);

// AFTER (excludes expired tokens)
var token = await _dbContext.RefreshTokens
    .FirstOrDefaultAsync(t =>
        t.Token == refreshToken &&
        t.IsActive &&
        t.ExpiresAt > DateTimeOffset.UtcNow);  // ✅ Check expiration
```

**Impact:** Prevents token refresh with already-expired refresh tokens.

---

**Doc:** [backend/architecture/tenant-id-interceptor.md](backend/architecture/tenant-id-interceptor.md)

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                        NOIR.Web                                  │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐   │
│  │  Endpoints   │  │  Middleware  │  │  frontend/ (React)   │   │
│  └──────────────┘  └──────────────┘  └──────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    NOIR.Infrastructure                           │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐   │
│  │  Persistence │  │   Identity   │  │      Services        │   │
│  │  (EF Core)   │  │  (Auth/JWT)  │  │  (Email, Storage)    │   │
│  └──────────────┘  └──────────────┘  └──────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                     NOIR.Application                             │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐   │
│  │   Features   │  │ Specifications│  │     Behaviors        │   │
│  │(Commands/Queries)│ │              │  │   (Middleware)       │   │
│  └──────────────┘  └──────────────┘  └──────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                       NOIR.Domain                                │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐   │
│  │   Entities   │  │  Interfaces  │  │   Common/ValueObjects │   │
│  └──────────────┘  └──────────────┘  └──────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

### Dependency Flow

```
Domain ← Application ← Infrastructure ← Web
```

- **Domain**: No external dependencies (pure C#)
- **Application**: Depends only on Domain
- **Infrastructure**: Implements Application interfaces
- **Web**: Composes all layers

---

## Domain Layer

**Path:** `src/NOIR.Domain/`

### Base Classes

| Class | Path | Purpose |
|-------|------|---------|
| `Entity<TId>` | `Common/Entity.cs` | Base entity with Id, CreatedAt, ModifiedAt |
| `AuditableEntity<TId>` | `Common/AuditableEntity.cs` | Entity with full audit fields |
| `AggregateRoot<TId>` | `Common/AggregateRoot.cs` | DDD aggregate root with domain events |
| `PlatformTenantEntity<TId>` | `Common/PlatformTenantEntity.cs` | Entity with platform/tenant pattern (no domain events) |
| `PlatformTenantAggregateRoot<TId>` | `Common/PlatformTenantAggregateRoot.cs` | Aggregate root with platform/tenant pattern |
| `ValueObject` | `Common/ValueObject.cs` | Immutable value object base |
| `Result<T>` | `Common/Result.cs` | Railway-oriented error handling |

### Entities

#### Core Entities

| Entity | Path | Related To |
|--------|------|------------|
| `Permission` | `Entities/Permission.cs` | [RBAC Authorization](#authorization) |
| `PermissionTemplate` | `Entities/PermissionTemplate.cs` | Role permission presets |
| `RefreshToken` | `Entities/RefreshToken.cs` | [JWT Pattern](backend/patterns/jwt-refresh-token.md) |
| `ResourceShare` | `Entities/ResourceShare.cs` | Multi-user sharing |

#### Audit Entities

| Entity | Path | Related To |
|--------|------|------------|
| `EntityAuditLog` | `Entities/EntityAuditLog.cs` | [Audit Logging](backend/patterns/hierarchical-audit-logging.md) |
| `HandlerAuditLog` | `Entities/HandlerAuditLog.cs` | [Audit Logging](backend/patterns/hierarchical-audit-logging.md) |
| `HttpRequestAuditLog` | `Entities/HttpRequestAuditLog.cs` | [Audit Logging](backend/patterns/hierarchical-audit-logging.md) |

#### Multi-Tenancy Entities

| Entity | Path | Related To |
|--------|------|------------|
| `Tenant` | `Entities/Tenant.cs` | [Multi-Tenancy](#multi-tenancy) |
| `TenantBranding` | `Entities/TenantBranding.cs` | Tenant customization |
| `TenantDomain` | `Entities/TenantDomain.cs` | Custom tenant domains |
| `TenantSetting` | `Entities/TenantSetting.cs` | Tenant configuration |
| `UserTenantMembership` | `Entities/UserTenantMembership.cs` | [Multi-Tenant User Access](#multi-tenancy) |

#### Notification Entities

| Entity | Path | Related To |
|--------|------|------------|
| `Notification` | `Entities/Notification.cs` | [Notifications Feature](#notifications-feature) |
| `NotificationPreference` | `Entities/NotificationPreference.cs` | User notification settings |
| `EmailTemplate` | `Entities/EmailTemplate.cs` | [Email Templates Feature](#emailtemplates-feature) |

#### Blog Entities

| Entity | Path | Related To |
|--------|------|------------|
| `Post` | `Entities/Post.cs` | [Blog Feature](#blog-feature-cms) |
| `PostCategory` | `Entities/PostCategory.cs` | [Blog Feature](#blog-feature-cms) |
| `PostTag` | `Entities/PostTag.cs` | [Blog Feature](#blog-feature-cms) |

#### Authentication Entities

| Entity | Path | Related To |
|--------|------|------------|
| `EmailChangeOtp` | `Entities/EmailChangeOtp.cs` | Email change verification |
| `PasswordResetOtp` | `Entities/PasswordResetOtp.cs` | Password reset flow |

#### E-commerce Entities

| Entity | Path | Base Class |
|--------|------|------------|
| `Product` | `Entities/Product/Product.cs` | `TenantAggregateRoot<Guid>` |
| `ProductVariant` | `Entities/Product/ProductVariant.cs` | `TenantEntity<Guid>` |
| `ProductImage` | `Entities/Product/ProductImage.cs` | `TenantEntity<Guid>` |
| `ProductCategory` | `Entities/Product/ProductCategory.cs` | `TenantAggregateRoot<Guid>` |
| `ProductOption` | `Entities/Product/ProductOption.cs` | `TenantEntity<Guid>` |
| `ProductOptionValue` | `Entities/Product/ProductOptionValue.cs` | `TenantEntity<Guid>` |
| `ProductAttribute` | `Entities/Product/ProductAttribute.cs` | `TenantAggregateRoot<Guid>` |
| `ProductAttributeValue` | `Entities/Product/ProductAttributeValue.cs` | `TenantEntity<Guid>` |
| `ProductAttributeAssignment` | `Entities/Product/ProductAttributeAssignment.cs` | `TenantEntity<Guid>` |
| `ProductFilterIndex` | `Entities/Product/ProductFilterIndex.cs` | `TenantEntity<Guid>` |
| `Brand` | `Entities/Product/Brand.cs` | `TenantAggregateRoot<Guid>` |
| `InventoryMovement` | `Entities/Product/InventoryMovement.cs` | `TenantAggregateRoot<Guid>` |

#### Cart & Checkout Entities

| Entity | Path | Base Class |
|--------|------|------------|
| `Cart` | `Entities/Cart/Cart.cs` | `TenantAggregateRoot<Guid>` |
| `CartItem` | `Entities/Cart/CartItem.cs` | `TenantEntity<Guid>` |
| `CheckoutSession` | `Entities/Checkout/CheckoutSession.cs` | `TenantAggregateRoot<Guid>` |

#### Order Entities

| Entity | Path | Base Class |
|--------|------|------------|
| `Order` | `Entities/Order/Order.cs` | `TenantAggregateRoot<Guid>` |
| `OrderItem` | `Entities/Order/OrderItem.cs` | `TenantEntity<Guid>` |

#### Shipping Entities

| Entity | Path | Base Class |
|--------|------|------------|
| `ShippingProvider` | `Entities/Shipping/ShippingProvider.cs` | `TenantAggregateRoot<Guid>` |
| `ShippingOrder` | `Entities/Shipping/ShippingOrder.cs` | `TenantAggregateRoot<Guid>` |
| `ShippingTrackingEvent` | `Entities/Shipping/ShippingTrackingEvent.cs` | `TenantEntity<Guid>` |
| `ShippingWebhookLog` | `Entities/Shipping/ShippingWebhookLog.cs` | `Entity<Guid>` |

#### Payment Entities

| Entity | Path | Base Class |
|--------|------|------------|
| `PaymentGateway` | `Entities/Payment/PaymentGateway.cs` | `TenantAggregateRoot<Guid>` |
| `PaymentTransaction` | `Entities/Payment/PaymentTransaction.cs` | `TenantAggregateRoot<Guid>` |
| `PaymentWebhookLog` | `Entities/Payment/PaymentWebhookLog.cs` | `TenantAggregateRoot<Guid>` |
| `PaymentOperationLog` | `Entities/Payment/PaymentOperationLog.cs` | `TenantAggregateRoot<Guid>` |
| `PaymentInstallment` | `Entities/Payment/PaymentInstallment.cs` | `TenantEntity<Guid>` |
| `Refund` | `Entities/Payment/Refund.cs` | [Payment Feature](#payments-feature-new) |

#### Inventory Entities

| Entity | Path | Base Class |
|--------|------|------------|
| `InventoryReceipt` | `Entities/Inventory/InventoryReceipt.cs` | `TenantAggregateRoot<Guid>` |
| `InventoryReceiptItem` | `Entities/Inventory/InventoryReceiptItem.cs` | `TenantEntity<Guid>` |

#### Analytics Entities

| Entity | Path | Base Class |
|--------|------|------------|
| `FilterAnalyticsEvent` | `Entities/Analytics/FilterAnalyticsEvent.cs` | `TenantEntity<Guid>` |

#### Legal & Content Entities

| Entity | Path | Base Class |
|--------|------|------------|
| `LegalPage` | `Entities/LegalPage.cs` | `PlatformTenantAggregateRoot<Guid>` |

#### Identity Entities (Infrastructure Layer)

| Entity | Path | Related To |
|--------|------|------------|
| `ApplicationUser` | `Infrastructure/Identity/ApplicationUser.cs` | User with extended properties (LockedAt, LockedBy) |
| `ApplicationRole` | `Infrastructure/Identity/ApplicationRole.cs` | Role with hierarchy (ParentRoleId, IconName, Color) |

### Interfaces

| Interface | Path | Implementation |
|-----------|------|----------------|
| `IRepository<TEntity, TId>` | `Interfaces/IRepository.cs` | `Repository<>` in Infrastructure |
| `IReadRepository<TEntity, TId>` | `Interfaces/IReadRepository.cs` | Read-only queries |
| `ISpecification<T>` | `Interfaces/ISpecification.cs` | Query specifications |
| `IUnitOfWork` | `Interfaces/IUnitOfWork.cs` | Transaction boundary |

### Constants

| Constant | Path | Purpose |
|----------|------|---------|
| `Permissions` | `Common/Permissions.cs` | Permission string constants |
| `Roles` | `Common/Roles.cs` | System role constants |
| `DatabaseConstants` | `Common/DatabaseConstants.cs` | Database schema constants (TenantIdMaxLength, UserIdMaxLength) |

### Platform/Tenant Pattern

**Pattern**: Platform defaults with tenant overrides and copy-on-edit semantics.

#### Base Classes

**PlatformTenantEntity<TId>**: For entities without domain events
- Inherits: `Entity<TId>`, implements `IAuditableEntity`
- Properties: `TenantId` (nullable), `IsPlatformDefault`, `IsTenantOverride`
- Includes: Full audit fields (CreatedBy, ModifiedBy, DeletedBy, IsDeleted, DeletedAt)

**PlatformTenantAggregateRoot<TId>**: For entities with domain events
- Inherits: `AggregateRoot<TId>`
- Properties: `TenantId` (nullable), `IsPlatformDefault`, `IsTenantOverride`
- Includes: Full audit fields + domain event management

#### Semantics

- **Platform Default** (`TenantId = null`): Shared across all tenants
- **Tenant Override** (`TenantId = value`): Tenant-specific customization
- **Copy-on-Edit**: Tenants create copies of platform entities when customizing

#### Examples

| Entity | Base Class | Usage |
|--------|-----------|-------|
| `EmailTemplate` | `PlatformTenantAggregateRoot<Guid>` | Platform email templates with tenant customization |
| `TenantSetting` | `PlatformTenantEntity<Guid>` | Platform settings with tenant overrides |
| `PermissionTemplate` | `PlatformTenantEntity<Guid>` | Platform permission templates |

#### Factory Methods

```csharp
// Semantic clarity - platform defaults
EmailTemplate.CreatePlatformDefault(name, subject, htmlBody, ...);

// Semantic clarity - tenant overrides
EmailTemplate.CreateTenantOverride(tenantId, name, subject, htmlBody, ...);
```

#### Database Optimization

**Filtered Indexes**: Platform default lookups are the most frequent queries, optimized with:
```sql
CREATE INDEX IX_EntityName_Platform_Lookup
ON EntityTable (Name, IsActive)
WHERE TenantId IS NULL AND IsDeleted = 0;
```

**Benefits**:
- 2-3x faster platform default queries
- 95% smaller index size (excludes tenant-specific rows)

**Schema Consistency**: All platform/tenant entities use `DatabaseConstants.TenantIdMaxLength = 64`

#### Smart Seed Updates (ISeedableEntity)

Entities that implement `ISeedableEntity` support version-based seed updates:
- `Version = 1`: Never modified, safe to update during seeding
- `Version > 1`: User-customized, skip seed updates

---

## Application Layer

**Path:** `src/NOIR.Application/`

### Feature Modules

#### Auth Feature
**Path:** `Features/Auth/`

| Type | Name | Path |
|------|------|------|
| Command | `LoginCommand` | `Commands/Login/` |
| Command | `RefreshTokenCommand` | `Commands/RefreshToken/` |
| Command | `LogoutCommand` | `Commands/Logout/` |
| Command | `UpdateUserProfileCommand` | `Commands/UpdateUserProfile/` |
| Query | `GetCurrentUserQuery` | `Queries/GetCurrentUser/` |
| Query | `GetUserByIdQuery` | `Queries/GetUserById/` |
| DTO | `AuthResponse` | `DTOs/AuthResponse.cs` |

**OTP Flow Canonical Pattern:**

All OTP-based features (Password Reset, Email Change, Phone Verification, etc.) MUST follow the canonical pattern for consistency and security:

**Reference Implementation:** `PasswordResetService.cs`

**Key Requirements** (from CLAUDE.md Critical Rule #14):
1. **Backend bypass prevention:** When user requests OTP with same target:
   - If cooldown active → Return existing session (no new OTP, no email)
   - If cooldown passed, same target → Use `ResendOtpInternalAsync` (keeps sessionToken, new OTP)
   - If cooldown passed, different target → Mark old OTP used, create new session
2. **Frontend error handling:** Clear OTP input on verification error (use `useEffect` watching `serverError`)
3. **Session token stability:** Use refs (`sessionTokenRef`) to avoid stale closures

**Why This Matters:**
- Prevents OTP bypass attacks (reusing old session tokens)
- Consistent UX across all OTP features
- Prevents rate limit abuse

**See:** CLAUDE.md Critical Rule #14 for complete pattern details

**Related:** [AuthEndpoints](#auth-endpoints), [TokenService](#identity-services)

#### Users Feature
**Path:** `Features/Users/`

| Type | Name | Path |
|------|------|------|
| Command | `CreateUserCommand` | `Commands/CreateUser/` |
| Command | `UpdateUserCommand` | `Commands/UpdateUser/` |
| Command | `DeleteUserCommand` | `Commands/DeleteUser/` |
| Command | `AssignRolesToUserCommand` | `Commands/AssignRoles/` |
| Command | `LockUserCommand` | `Commands/LockUser/` |
| Query | `GetUsersQuery` | `Queries/GetUsers/` |
| Query | `GetUserRolesQuery` | `Queries/GetUserRoles/` |
| DTO | `UserDtos` | `DTOs/UserDtos.cs` |

**Related:** [UserEndpoints](#user-endpoints)

#### Roles Feature
**Path:** `Features/Roles/`

| Type | Name | Path |
|------|------|------|
| Command | `CreateRoleCommand` | `Commands/CreateRole/` |
| Command | `UpdateRoleCommand` | `Commands/UpdateRole/` |
| Command | `DeleteRoleCommand` | `Commands/DeleteRole/` |
| Query | `GetRolesQuery` | `Queries/GetRoles/` |
| Query | `GetRoleByIdQuery` | `Queries/GetRoleById/` |
| DTO | `RoleDtos` | `DTOs/RoleDtos.cs` |

**Related:** [RoleEndpoints](#role-endpoints)

#### Permissions Feature
**Path:** `Features/Permissions/`

| Type | Name | Path |
|------|------|------|
| Command | `AssignPermissionToRoleCommand` | `Commands/AssignToRole/` |
| Command | `RemovePermissionFromRoleCommand` | `Commands/RemoveFromRole/` |
| Query | `GetRolePermissionsQuery` | `Queries/GetRolePermissions/` |
| Query | `GetUserPermissionsQuery` | `Queries/GetUserPermissions/` |
| Query | `GetAllPermissionsQuery` | `Queries/GetAllPermissions/` |
| Query | `GetPermissionTemplatesQuery` | `Queries/GetPermissionTemplates/` |

**Related:** [Authorization](#authorization), [PermissionEndpoints](#permission-endpoints)

#### EmailTemplates Feature
**Path:** `Features/EmailTemplates/`

| Type | Name | Path |
|------|------|------|
| Command | `UpdateEmailTemplateCommand` | `Commands/Update/` |
| Query | `GetEmailTemplatesQuery` | `Queries/GetAll/` |
| Query | `GetEmailTemplateByIdQuery` | `Queries/GetById/` |

**Service:** `EmailService` (`Infrastructure/Services/EmailService.cs`)
- Uses **platform-level fallback**: tenant-specific template → platform template (TenantId = null)
- Templates seeded as platform-level by default via `ApplicationDbContextSeeder`
- Supports variable replacement: `{{DisplayName}}`, `{{Email}}`, `{{Password}}`, etc.

**Related:** [EmailTemplateEndpoints](#email-template-endpoints), [Platform-Level Data](#platform-level-vs-tenant-level-data)

#### Notifications Feature
**Path:** `Features/Notifications/`

| Type | Name | Path |
|------|------|------|
| Command | `MarkAsReadCommand` | `Commands/MarkAsRead/` |
| Command | `MarkAllAsReadCommand` | `Commands/MarkAllAsRead/` |
| Command | `DeleteNotificationCommand` | `Commands/DeleteNotification/` |
| Command | `UpdatePreferencesCommand` | `Commands/UpdatePreferences/` |
| Query | `GetNotificationsQuery` | `Queries/GetNotifications/` |
| Query | `GetUnreadCountQuery` | `Queries/GetUnreadCount/` |
| Query | `GetPreferencesQuery` | `Queries/GetPreferences/` |

**Related:** [NotificationEndpoints](#notification-endpoints)

#### Tenants Feature
**Path:** `Features/Tenants/`

| Type | Name | Path |
|------|------|------|
| Command | `CreateTenantCommand` | `Commands/Create/` |
| Command | `UpdateTenantCommand` | `Commands/Update/` |
| Command | `DeleteTenantCommand` | `Commands/Delete/` |
| Query | `GetTenantsQuery` | `Queries/GetAll/` |
| Query | `GetTenantByIdQuery` | `Queries/GetById/` |

**Related:** [TenantEndpoints](#tenant-endpoints)

#### Payments Feature (NEW)
**Path:** `Features/Payments/`

| Type | Name | Path |
|------|------|------|
| Command | `CreatePaymentCommand` | `Commands/CreatePayment/` |
| Command | `CancelPaymentCommand` | `Commands/CancelPayment/` |
| Command | `ConfigureGatewayCommand` | `Commands/ConfigureGateway/` |
| Command | `UpdateGatewayCommand` | `Commands/UpdateGateway/` |
| Command | `ProcessWebhookCommand` | `Commands/ProcessWebhook/` |
| Command | `RequestRefundCommand` | `Commands/RequestRefund/` |
| Command | `ApproveRefundCommand` | `Commands/ApproveRefund/` |
| Command | `RejectRefundCommand` | `Commands/RejectRefund/` |
| Command | `ConfirmCodCollectionCommand` | `Commands/ConfirmCodCollection/` |
| Query | `GetPaymentTransactionsQuery` | `Queries/GetPaymentTransactions/` |
| Query | `GetPaymentTransactionQuery` | `Queries/GetPaymentTransaction/` |
| Query | `GetOrderPaymentsQuery` | `Queries/GetOrderPayments/` |
| Query | `GetPaymentGatewaysQuery` | `Queries/GetPaymentGateways/` |
| Query | `GetPaymentGatewayQuery` | `Queries/GetPaymentGateway/` |
| Query | `GetActiveGatewaysQuery` | `Queries/GetActiveGateways/` |
| Query | `GetRefundsQuery` | `Queries/GetRefunds/` |
| Query | `GetPendingCodPaymentsQuery` | `Queries/GetPendingCodPayments/` |
| Query | `GetWebhookLogsQuery` | `Queries/GetWebhookLogs/` |
| DTO | `PaymentGatewayDto`, `PaymentTransactionDto`, `RefundDto`, `WebhookLogDto` | `DTOs/` |
| Spec | `PaymentGatewaySpecs`, `PaymentTransactionSpecs`, `WebhookLogSpecs` | `Specifications/` |

**Service Interfaces:**
- `IPaymentService` - Payment orchestration
- `IPaymentGatewayFactory` - Gateway provider instantiation
- `IPaymentGatewayProvider` - Gateway-specific implementation
- `ICredentialEncryptionService` - Secure credential storage

**Domain Entities:**
- `PaymentGateway` - Per-tenant gateway configuration with encrypted credentials
- `PaymentTransaction` - Full payment lifecycle tracking
- `PaymentWebhookLog` - Webhook audit trail with processing status
- `Refund` - Refund tracking with approval workflow

**Enums:**
- `PaymentStatus` - Pending, Processing, Authorized, Paid, Failed, Cancelled, Refunded, CodPending
- `PaymentMethod` - CreditCard, DebitCard, EWallet, QRCode, BankTransfer, COD, BuyNowPayLater
- `RefundStatus` - Pending, Approved, Processing, Completed, Rejected, Failed
- `GatewayEnvironment` - Sandbox, Production
- `GatewayHealthStatus` - Unknown, Healthy, Degraded, Unhealthy

**Related:** [PaymentEndpoints](#payment-endpoints), [FEATURE_CATALOG - Payment Processing](FEATURE_CATALOG.md#payment-processing)

#### Blog Feature (CMS)
**Path:** `Features/Blog/`

| Type | Name | Path |
|------|------|------|
| Command | `CreatePostCommand` | `Commands/CreatePost/` |
| Command | `UpdatePostCommand` | `Commands/UpdatePost/` |
| Command | `DeletePostCommand` | `Commands/DeletePost/` |
| Command | `PublishPostCommand` | `Commands/PublishPost/` |
| Command | `CreateCategoryCommand` | `Commands/CreateCategory/` |
| Command | `UpdateCategoryCommand` | `Commands/UpdateCategory/` |
| Command | `DeleteCategoryCommand` | `Commands/DeleteCategory/` |
| Command | `CreateTagCommand` | `Commands/CreateTag/` |
| Command | `UpdateTagCommand` | `Commands/UpdateTag/` |
| Command | `DeleteTagCommand` | `Commands/DeleteTag/` |
| Query | `GetPostsQuery` | `Queries/GetPosts/` |
| Query | `GetPostQuery` | `Queries/GetPost/` |
| Query | `GetCategoriesQuery` | `Queries/GetCategories/` |
| Query | `GetTagsQuery` | `Queries/GetTags/` |
| DTO | `PostListDto`, `PostDetailDto`, `PostCategoryDto`, `PostTagDto` | `DTOs/BlogDtos.cs` |
| Spec | `PostSpecifications`, `CategorySpecifications`, `TagSpecifications` | `Specifications/` |

**Related:** [BlogEndpoints](#blog-endpoints), [Blog Pages](#blog-pages)

**Domain Entities:**
- `Post` - Blog post with title, slug, content, excerpt, featured image, SEO metadata
- `PostCategory` - Hierarchical categories with slug and sort order
- `PostTag` - Tags with optional color for visual distinction

#### DeveloperLogs Feature
**Path:** `Features/DeveloperLogs/`

| Type | Name | Path |
|------|------|------|
| DTO | `LogEntryDto` | `DTOs/LogEntryDto.cs` |

**Note:** Developer logs are streamed via SignalR hub, not traditional API endpoints.

**Related:** [DeveloperLogEndpoints](#developer-log-endpoints), [Developer Logs Page](#developer-logs-page)

#### Audit Feature
**Path:** `Features/Audit/`

| Type | Name | Path |
|------|------|------|
| Query | `SearchActivityTimelineQuery` | `Queries/SearchActivityTimeline/` |
| Query | `GetActivityDetailsQuery` | `Queries/GetActivityDetails/` |
| Query | `GetAuditableEntityTypesQuery` | `Queries/GetAuditableEntityTypes/` |
| Query | `GetEntityHistoryQuery` | `Queries/GetEntityHistory/` |
| Query | `GetEntityVersionsQuery` | `Queries/GetEntityVersions/` |
| Query | `SearchEntitiesWithHistoryQuery` | `Queries/SearchEntitiesWithHistory/` |
| DTO | `ActivityTimelineEntryDto` | `DTOs/ActivityTimelineEntryDto.cs` |
| DTO | `EntityHistoryEntryDto` | `DTOs/EntityHistoryEntryDto.cs` |
| DTO | `EntityVersionDto` | `DTOs/EntityVersionDto.cs` |
| DTO | `EntitySearchResultDto` | `DTOs/EntitySearchResultDto.cs` |
| DTO | `FieldChangeDto` | `DTOs/FieldChangeDto.cs` |

**Related:** [AuditEndpoints](#audit-endpoints), [Activity Timeline](#activity-timeline-page)

**Activity Timeline Page:** `frontend/src/portal-app/systems/features/activity-timeline/`
- Hierarchical audit log viewer with correlation tracking
- Date range filtering with `DateRangePicker`
- User activity navigation (click user → filter by user)
- Search across entity ID, user email, handler name, field values
- Expandable rows showing entity changes (before/after diff)

#### Products Feature
**Path:** `Features/Products/`

| Type | Name | Path |
|------|------|------|
| Command | `CreateProductCommand` | `Commands/CreateProduct/` |
| Command | `UpdateProductCommand` | `Commands/UpdateProduct/` |
| Command | `DeleteProductCommand` | `Commands/DeleteProduct/` |
| Command | `PublishProductCommand` | `Commands/PublishProduct/` |
| Command | `ArchiveProductCommand` | `Commands/ArchiveProduct/` |
| Command | `DuplicateProductCommand` | `Commands/DuplicateProduct/` |
| Command | `BulkPublishProductsCommand` | `Commands/BulkPublishProducts/` |
| Command | `BulkArchiveProductsCommand` | `Commands/BulkArchiveProducts/` |
| Command | `BulkDeleteProductsCommand` | `Commands/BulkDeleteProducts/` |
| Command | `BulkImportProductsCommand` | `Commands/BulkImportProducts/` |
| Command | `AddProductVariantCommand` | `Commands/AddProductVariant/` |
| Command | `UpdateProductVariantCommand` | `Commands/UpdateProductVariant/` |
| Command | `DeleteProductVariantCommand` | `Commands/DeleteProductVariant/` |
| Command | `AddProductImageCommand` | `Commands/AddProductImage/` |
| Command | `UpdateProductImageCommand` | `Commands/UpdateProductImage/` |
| Command | `DeleteProductImageCommand` | `Commands/DeleteProductImage/` |
| Command | `UploadProductImageCommand` | `Commands/UploadProductImage/` |
| Command | `SetPrimaryProductImageCommand` | `Commands/SetPrimaryProductImage/` |
| Command | `ReorderProductImagesCommand` | `Commands/ReorderProductImages/` |
| Command | `AddProductOptionCommand` | `Commands/AddProductOption/` |
| Command | `UpdateProductOptionCommand` | `Commands/UpdateProductOption/` |
| Command | `DeleteProductOptionCommand` | `Commands/DeleteProductOption/` |
| Command | `AddProductOptionValueCommand` | `Commands/AddProductOptionValue/` |
| Command | `UpdateProductOptionValueCommand` | `Commands/UpdateProductOptionValue/` |
| Command | `DeleteProductOptionValueCommand` | `Commands/DeleteProductOptionValue/` |
| Command | `CreateProductCategoryCommand` | `Commands/CreateProductCategory/` |
| Command | `UpdateProductCategoryCommand` | `Commands/UpdateProductCategory/` |
| Command | `DeleteProductCategoryCommand` | `Commands/DeleteProductCategory/` |
| Query | `GetProductsQuery` | `Queries/GetProducts/` |
| Query | `GetProductByIdQuery` | `Queries/GetProductById/` |
| Query | `GetProductStatsQuery` | `Queries/GetProductStats/` |
| Query | `GetProductCategoriesQuery` | `Queries/GetProductCategories/` |
| Query | `GetProductCategoryByIdQuery` | `Queries/GetProductCategoryById/` |
| Query | `GetProductOptionByIdQuery` | `Queries/GetProductOptionById/` |
| Query | `GetProductOptionValueByIdQuery` | `Queries/GetProductOptionValueById/` |
| Query | `ExportProductsQuery` | `Queries/ExportProducts/` |

**Related:** [ProductEndpoints](#product-endpoints), [ProductCategoryEndpoints](#product-category-endpoints)

**Domain Entities:**
- `Product` - Core product with name, slug, description, SEO metadata, status (Draft/Active/Archived)
- `ProductVariant` - SKU-level variants with price, inventory, weight
- `ProductImage` - Multiple images with display order, alt text, ThumbHash
- `ProductCategory` - Hierarchical categories with parent-child relationships
- `ProductOption` - Configurable options (Size, Color, Material)
- `ProductOptionValue` - Option values (Small, Medium, Large)

#### Brands Feature
**Path:** `Features/Brands/`

| Type | Name | Path |
|------|------|------|
| Command | `CreateBrandCommand` | `Commands/CreateBrand/` |
| Command | `UpdateBrandCommand` | `Commands/UpdateBrand/` |
| Command | `DeleteBrandCommand` | `Commands/DeleteBrand/` |
| Query | `GetBrandsQuery` | `Queries/GetBrands/` |
| Query | `GetBrandByIdQuery` | `Queries/GetBrandById/` |
| DTO | `BrandDto`, `BrandListDto` | `DTOs/BrandDtos.cs` |
| Spec | `BrandSpecifications` | `Specifications/BrandSpecifications.cs` |

**Related:** [BrandEndpoints](#brand-endpoints)

**Domain Entity:** `Brand` - Brand with name, slug, logo URL, banner URL, description, SEO metadata, featured status

#### ProductAttributes Feature
**Path:** `Features/ProductAttributes/`

| Type | Name | Path |
|------|------|------|
| Command | `CreateProductAttributeCommand` | `Commands/CreateProductAttribute/` |
| Command | `UpdateProductAttributeCommand` | `Commands/UpdateProductAttribute/` |
| Command | `DeleteProductAttributeCommand` | `Commands/DeleteProductAttribute/` |
| Command | `AddProductAttributeValueCommand` | `Commands/AddProductAttributeValue/` |
| Command | `UpdateProductAttributeValueCommand` | `Commands/UpdateProductAttributeValue/` |
| Command | `RemoveProductAttributeValueCommand` | `Commands/RemoveProductAttributeValue/` |
| Command | `AssignAttributeToCategoryCommand` | `Commands/AssignAttributeToCategory/` |
| Command | `RemoveAttributeFromCategoryCommand` | `Commands/RemoveAttributeFromCategory/` |
| Command | `AssignAttributeToProductCommand` | `Commands/AssignAttributeToProduct/` |
| Query | `GetProductAttributesQuery` | `Queries/GetProductAttributes/` |
| Query | `GetProductAttributeByIdQuery` | `Queries/GetProductAttributeById/` |
| Query | `GetCategoryAttributesQuery` | `Queries/GetCategoryAttributes/` |
| Query | `GetProductAttributeFormSchemaQuery` | `Queries/GetProductAttributeFormSchema/` |
| Query | `GetCategoryAttributeFormSchemaQuery` | `Queries/GetCategoryAttributeFormSchema/` |
| DTO | `ProductAttributeDto`, `ProductAttributeValueDto`, `CategoryAttributeDto` | `DTOs/ProductAttributeDtos.cs` |
| Spec | `ProductAttributeSpecifications`, `CategoryAttributeSpecifications` | `Specifications/` |

**Related:** [ProductAttributeEndpoints](#product-attribute-endpoints)

**13 Attribute Types:** Select, MultiSelect, Text, TextArea, Number, Decimal, Boolean, Date, DateTime, Color, Range, Url, File

**Domain Entities:**
- `ProductAttribute` - Attribute definition with type, validation rules
- `ProductAttributeValue` - Predefined values for Select/MultiSelect attributes
- `CategoryAttribute` - M:N linkage between categories and attributes
- `ProductAttributeAssignment` - Actual attribute values assigned to products

#### Cart Feature
**Path:** `Features/Cart/`

| Type | Name | Path |
|------|------|------|
| Command | `AddToCartCommand` | `Commands/AddToCart/` |
| Command | `UpdateCartItemCommand` | `Commands/UpdateCartItem/` |
| Command | `RemoveCartItemCommand` | `Commands/RemoveCartItem/` |
| Command | `ClearCartCommand` | `Commands/ClearCart/` |
| Command | `MergeCartCommand` | `Commands/MergeCart/` |
| Query | `GetCartQuery` | `Queries/GetCart/` |
| Query | `GetCartByIdQuery` | `Queries/GetCartById/` |
| Query | `GetCartSummaryQuery` | `Queries/GetCartSummary/` |
| DTO | `CartDto`, `CartItemDto`, `CartSummaryDto`, `CartMergeResultDto` | `DTOs/CartDtos.cs` |
| Spec | `CartSpecifications` | `Specifications/CartSpecifications.cs` |

**Related:** [CartEndpoints](#cart-endpoints)

**Key Features:**
- Supports both authenticated users (UserId) and guest sessions (SessionId via cookie)
- Guest carts can be merged with user carts on login via `MergeCartCommand`
- CartStatus: Active → Converted (on checkout) or Abandoned (cleanup)

#### Checkout Feature
**Path:** `Features/Checkout/`

| Type | Name | Path |
|------|------|------|
| Command | `InitiateCheckoutCommand` | `Commands/InitiateCheckout/` |
| Command | `SetCheckoutAddressCommand` | `Commands/SetCheckoutAddress/` |
| Command | `SelectShippingMethodCommand` | `Commands/SelectShippingMethod/` |
| Command | `SelectPaymentMethodCommand` | `Commands/SelectPaymentMethod/` |
| Command | `CompleteCheckoutCommand` | `Commands/CompleteCheckout/` |
| Query | `GetCheckoutSessionQuery` | `Queries/GetCheckoutSession/` |
| DTO | `CheckoutSessionDto`, `InitiateCheckoutRequest`, `SetCheckoutAddressRequest` | `DTOs/CheckoutDtos.cs` |
| Spec | `CheckoutSpecs` | `Specifications/CheckoutSpecs.cs` |

**Related:** [CheckoutEndpoints](#checkout-endpoints)

**Checkout Flow:**
1. `InitiateCheckout` - Creates session from cart
2. `SetCheckoutAddress` - Shipping/billing address (Vietnam format)
3. `SelectShippingMethod` - Shipping carrier selection
4. `SelectPaymentMethod` - Payment gateway + method
5. `CompleteCheckout` - Creates Order, reserves inventory

#### Orders Feature
**Path:** `Features/Orders/`

| Type | Name | Path |
|------|------|------|
| Command | `CreateOrderCommand` | `Commands/CreateOrder/` |
| Command | `ConfirmOrderCommand` | `Commands/ConfirmOrder/` |
| Command | `ShipOrderCommand` | `Commands/ShipOrder/` |
| Command | `DeliverOrderCommand` | `Commands/DeliverOrder/` |
| Command | `CompleteOrderCommand` | `Commands/CompleteOrder/` |
| Command | `CancelOrderCommand` | `Commands/CancelOrder/` |
| Command | `ReturnOrderCommand` | `Commands/ReturnOrder/` |
| Query | `GetOrdersQuery` | `Queries/GetOrders/` |
| Query | `GetOrderByIdQuery` | `Queries/GetOrderById/` |
| DTO | `OrderDto`, `OrderSummaryDto`, `OrderItemDto`, `AddressDto` | `DTOs/OrderDtos.cs` |
| Spec | `OrderSpecs` | `Specifications/OrderSpecs.cs` |

**Related:** [OrderEndpoints](#order-endpoints)

**Order Lifecycle:** Pending → Confirmed → Processing → Shipped → Delivered → Completed (or Cancelled/Returned at various stages)

**Financial Fields:** SubTotal, DiscountAmount, ShippingAmount, TaxAmount, GrandTotal (VND currency)

#### Shipping Feature
**Path:** `Features/Shipping/`

| Type | Name | Path |
|------|------|------|
| Command | `CreateShippingProviderCommand` | `Commands/CreateShippingProvider/` |
| Command | `UpdateShippingProviderCommand` | `Commands/UpdateShippingProvider/` |
| Command | `DeleteShippingProviderCommand` | `Commands/DeleteShippingProvider/` |
| Command | `CreateShippingOrderCommand` | `Commands/CreateShippingOrder/` |
| Query | `GetShippingProvidersQuery` | `Queries/GetShippingProviders/` |
| Query | `GetShippingOrdersQuery` | `Queries/GetShippingOrders/` |
| Query | `CalculateShippingRatesQuery` | `Queries/CalculateShippingRates/` |
| DTO | `ShippingProviderDto`, `ShippingOrderDto`, `ShippingRateDto` | `DTOs/` |

**Related:** [ShippingEndpoints](#shipping-endpoints), [ShippingProviderEndpoints](#shipping-provider-endpoints)

**Supported Carriers (Vietnam):** GHTK, GHN, J&T Express, Viettel Post, NinjaVan, VNPost, BestExpress, Custom

**Enums:**
- `ShippingStatus` - Draft, Pending, PickingUp, InTransit, OutForDelivery, Delivered, Failed, Cancelled, Returning, Returned
- `ShippingProviderCode` - GHTK, GHN, JTExpress, ViettelPost, NinjaVan, VNPost, BestExpress, Custom
- `ShippingProviderHealthStatus` - Unknown, Healthy, Degraded, Unhealthy

#### Inventory Feature
**Path:** `Features/Inventory/`

| Type | Name | Path |
|------|------|------|
| Command | `CreateStockMovementCommand` | `Commands/CreateStockMovement/` |
| Command | `CreateInventoryReceiptCommand` | `Commands/CreateInventoryReceipt/` |
| Command | `ConfirmInventoryReceiptCommand` | `Commands/ConfirmInventoryReceipt/` |
| Command | `CancelInventoryReceiptCommand` | `Commands/CancelInventoryReceipt/` |
| Query | `GetStockHistoryQuery` | `Queries/GetStockHistory/` |
| Query | `GetInventoryReceiptsQuery` | `Queries/GetInventoryReceipts/` |
| Query | `GetInventoryReceiptByIdQuery` | `Queries/GetInventoryReceiptById/` |
| DTO | `InventoryMovementDto` | `DTOs/InventoryMovementDto.cs` |
| DTO | `InventoryReceiptDto`, `InventoryReceiptSummaryDto`, `InventoryReceiptItemDto`, `CreateInventoryReceiptItemDto` | `DTOs/InventoryReceiptDtos.cs` |
| Mapper | `InventoryMovementMapper` | `Mappers/InventoryMovementMapper.cs` |
| Mapper | `InventoryReceiptMapper` | `Mappers/InventoryReceiptMapper.cs` |
| Spec | `InventoryMovementSpecs` | `Specifications/InventoryMovementSpecs.cs` |
| Spec | `InventoryReceiptByIdSpec`, `InventoryReceiptByIdForUpdateSpec`, `InventoryReceiptsListSpec`, `InventoryReceiptsCountSpec`, `LatestReceiptNumberTodaySpec` | `Specifications/InventoryReceiptSpecs.cs` |

**Related:** [InventoryEndpoints](#inventory-endpoints)

**Key Concepts:**
- Inventory tracked at `ProductVariant` level. Movement types: StockIn, StockOut, Adjustment, Return, Reserved, Released
- **Inventory Receipts** (phieu nhap/xuat kho): Batch stock movement receipts with approval workflow
- Receipt number format: `RCV-YYYYMMDD-NNNN` (StockIn) or `SHP-YYYYMMDD-NNNN` (StockOut)
- Receipt status workflow: `Draft` → `Confirmed` (stock adjusted) or `Cancelled`
- Receipt types: `StockIn` (inbound), `StockOut` (outbound)
- `CreateStockMovementCommand` for individual manual movements
- `InventoryReceiptByIdForUpdateSpec` uses `AsTracking()` for mutation operations

**Domain Entities:**
- `InventoryReceipt` (`TenantAggregateRoot<Guid>`) - Receipt header with status/type/notes
- `InventoryReceiptItem` (`TenantEntity<Guid>`) - Line items with product snapshot (name, variant, SKU, quantity, unit cost)
- Computed: `TotalQuantity` (sum of item quantities), `TotalCost` (sum of item line totals), `LineTotal` (quantity x unit cost)

#### ProductFilter Feature
**Path:** `Features/ProductFilter/`

| Type | Name | Path |
|------|------|------|
| Query | `FilterProductsQuery` | `Queries/FilterProducts/` |
| Query | `GetCategoryFiltersQuery` | `Queries/GetCategoryFilters/` |
| DTO | `ProductFilterRequest`, `FilteredProductsResult`, `FacetsDto`, `CategoryFiltersDto` | `DTOs/FilterDtos.cs` |
| Spec | `ProductFilterSpecifications` | `Specifications/ProductFilterSpecifications.cs` |
| Service | `FacetCalculator` | `Services/FacetCalculator.cs` |

**Related:** [ProductFilterEndpoints](#product-filter-endpoints)

**Key Features:**
- Faceted filtering by category, brand, price range, attributes, stock status
- Dynamic facet calculation with counts per filter value
- Supports 4 display types: Checkbox, Color, Range, Boolean
- Sort options: newest, price, rating

#### ProductFilterIndex Feature
**Path:** `Features/ProductFilterIndex/`

| Type | Name | Path |
|------|------|------|
| Service | `AttributeJsonBuilder` | `Services/AttributeJsonBuilder.cs` |
| Handler | `ProductFilterIndexSyncHandler` | `EventHandlers/ProductFilterIndexSyncHandler.cs` |

**Purpose:** Denormalized index for fast faceted filtering. Syncs attribute data into a JSON column on `ProductFilterIndex` table when product attributes change.

#### FilterAnalytics Feature
**Path:** `Features/FilterAnalytics/`

| Type | Name | Path |
|------|------|------|
| Command | `CreateFilterEventCommand` | `Commands/CreateFilterEvent/` |
| Query | `GetPopularFiltersQuery` | `Queries/GetPopularFilters/` |
| DTO | `FilterAnalyticsEventDto`, `PopularFilterDto`, `PopularFiltersResult` | `DTOs/FilterAnalyticsDtos.cs` |

**Related:** [FilterAnalyticsEndpoints](#filter-analytics-endpoints)

**Purpose:** Tracks filter usage analytics (session, user, filter code/value, product count, conversion rate) for business intelligence.

#### LegalPages Feature
**Path:** `Features/LegalPages/`

| Type | Name | Path |
|------|------|------|
| Command | `UpdateLegalPageCommand` | `Commands/UpdateLegalPage/` |
| Command | `RevertLegalPageToDefaultCommand` | `Commands/RevertLegalPageToDefault/` |
| Query | `GetLegalPagesQuery` | `Queries/GetLegalPages/` |
| Query | `GetLegalPageQuery` | `Queries/GetLegalPage/` |
| Query | `GetPublicLegalPageQuery` | `Queries/GetPublicLegalPage/` |
| DTO | `LegalPageDto`, `LegalPageListDto`, `PublicLegalPageDto` | `DTOs/LegalPageDtos.cs` |

**Related:** [LegalPageEndpoints](#legal-page-endpoints), [PublicLegalPageEndpoints](#public-legal-page-endpoints)

**Key Features:**
- Platform/Tenant pattern with inheritance (platform defaults, tenant overrides)
- SEO metadata (MetaTitle, MetaDescription, CanonicalUrl, AllowIndexing)
- Public endpoint for website visitors (unauthenticated)
- Revert to platform default functionality

#### PlatformSettings Feature
**Path:** `Features/PlatformSettings/`

| Type | Name | Path |
|------|------|------|
| Command | `UpdateSmtpSettingsCommand` | `Commands/UpdateSmtpSettings/` |
| Command | `TestSmtpConnectionCommand` | `Commands/TestSmtpConnection/` |
| Query | `GetSmtpSettingsQuery` | `Queries/GetSmtpSettings/` |
| DTO | `SmtpSettingsDto` | `DTOs/SmtpSettingsDto.cs` |

**Related:** [PlatformSettingsEndpoints](#platform-settings-endpoints)

**Purpose:** Platform-level SMTP email configuration (host, port, credentials, TLS).

#### TenantSettings Feature
**Path:** `Features/TenantSettings/`

| Type | Name | Path |
|------|------|------|
| Command | `UpdateBrandingSettingsCommand` | `Commands/UpdateBrandingSettings/` |
| Command | `UpdateContactSettingsCommand` | `Commands/UpdateContactSettings/` |
| Command | `UpdateRegionalSettingsCommand` | `Commands/UpdateRegionalSettings/` |
| Command | `UpdateTenantSmtpSettingsCommand` | `Commands/UpdateTenantSmtpSettings/` |
| Command | `TestTenantSmtpConnectionCommand` | `Commands/TestTenantSmtpConnection/` |
| Command | `RevertTenantSmtpSettingsCommand` | `Commands/RevertTenantSmtpSettings/` |
| Query | `GetBrandingSettingsQuery` | `Queries/GetBrandingSettings/` |
| Query | `GetContactSettingsQuery` | `Queries/GetContactSettings/` |
| Query | `GetRegionalSettingsQuery` | `Queries/GetRegionalSettings/` |
| Query | `GetTenantSmtpSettingsQuery` | `Queries/GetTenantSmtpSettings/` |
| DTO | `BrandingSettingsDto`, `ContactSettingsDto`, `RegionalSettingsDto`, `TenantSmtpSettingsDto` | `DTOs/TenantSettingsDtos.cs` |

**Related:** [TenantSettingsEndpoints](#tenant-settings-endpoints)

**Settings Categories:**
- **Branding** - Logo, favicon, colors, dark mode default
- **Contact** - Email, phone, address
- **Regional** - Timezone, language, date format
- **SMTP** - Tenant-specific email configuration with inheritance from platform defaults

#### Media Feature
**Path:** `Features/Media/`

| Type | Name | Path |
|------|------|------|
| DTO | `MediaUploadResultDto` | `Dtos/MediaUploadResultDto.cs` |
| DTO | `MediaFileDto` | `Dtos/MediaFileDto.cs` |

**Related:** [MediaEndpoints](#media-endpoints), [FileEndpoints](#file-endpoints)

**Purpose:** Image upload and processing with automatic variant generation (Thumb 150px, ExtraLarge 1920px), WebP encoding, ThumbHash blur placeholders, and dominant color extraction.

#### Dashboard Feature
**Path:** `Features/Dashboard/`

| Type | Name | Path |
|------|------|------|
| Query | `GetDashboardMetricsQuery` | `Queries/GetDashboardMetrics/` |
| DTO | `DashboardMetricsDto`, `RevenueMetricsDto`, `OrderStatusCountsDto`, `TopSellingProductDto`, `LowStockProductDto`, `RecentOrderDto`, `SalesOverTimeDto`, `ProductStatusDistributionDto` | `DTOs/DashboardDtos.cs` |
| Service Interface | `IDashboardQueryService` | `IDashboardQueryService.cs` |
| Service Implementation | `DashboardQueryService` | `Infrastructure/Services/DashboardQueryService.cs` |

**Related:** [DashboardEndpoints](#dashboard-endpoints)

**Key Features:**
- Revenue metrics with period comparisons (today, this month, last month, total)
- Order counts by status (all 9 statuses)
- Top selling products by quantity sold (configurable count, default 5)
- Low stock products below threshold (configurable, default 10)
- Recent orders for activity feed (configurable count, default 10)
- Sales over time for chart rendering (configurable days, default 30)
- Product status distribution (Draft/Active/Archived)

**Architecture:**
- Handler delegates to `IDashboardQueryService` (Clean Architecture separation)
- Infrastructure implementation uses direct `DbContext` for efficient aggregation queries
- 7 independent queries run in parallel via `Task.WhenAll()` for performance
- All queries use `TagWith()` for SQL debugging (e.g., "Dashboard_TotalRevenue")
- Revenue excludes Cancelled/Refunded orders (only Confirmed through Completed)

### Specifications

**Path:** `Specifications/`
**Pattern Doc:** [Repository & Specification](backend/patterns/repository-specification.md)

All database queries MUST use specifications:
```csharp
public class ActiveUsersSpec : Specification<User>
{
    public ActiveUsersSpec()
    {
        Query.Where(u => u.IsActive)
             .TagWith("GetActiveUsers");  // REQUIRED
    }
}
```

### Behaviors (Pipeline)

**Path:** `Behaviors/`

| Behavior | Purpose |
|----------|---------|
| `LoggingMiddleware` | Request/response logging |
| `PerformanceMiddleware` | Performance metrics |

### Common Interfaces

**Path:** `Common/Interfaces/`

| Interface | Implementation | Purpose |
|-----------|----------------|---------|
| `ICurrentUser` | `CurrentUserService` | Current user context |
| `ITokenService` | `TokenService` | JWT generation |
| `IEmailService` | `EmailService` | Email sending |
| `IFileStorage` | `FileStorageService` | File operations |
| `IImageProcessor` | `ImageProcessorService` | Image processing and variants |
| `IDateTime` | `DateTimeService` | Testable date/time |
| `IDiffService` | `JsonDiffService` | Entity change diffs |

---

## Infrastructure Layer

**Path:** `src/NOIR.Infrastructure/`

### Persistence

**Path:** `Persistence/`
**Pattern Doc:** [Repository & Specification](backend/patterns/repository-specification.md)

| Component | Path | Purpose |
|-----------|------|---------|
| `ApplicationDbContext` | `Persistence/ApplicationDbContext.cs` | EF Core DbContext |
| `Repository<TEntity, TId>` | `Persistence/Repository.cs` | Generic repository |
| `SpecificationEvaluator` | `Persistence/SpecificationEvaluator.cs` | Specification evaluation |

#### Entity Configurations

**Path:** `Persistence/Configurations/`
**Pattern Doc:** [Entity Configuration](backend/patterns/entity-configuration.md)

Auto-discovered via `ApplyConfigurationsFromAssembly`.

#### Interceptors

**Path:** `Persistence/Interceptors/`

| Interceptor | Purpose |
|-------------|---------|
| `AuditableEntityInterceptor` | Sets audit fields (CreatedBy, ModifiedBy) |
| `DomainEventInterceptor` | Dispatches domain events |
| `TenantIdSetterInterceptor` | Multi-tenant isolation |
| `EntityAuditLogInterceptor` | Entity change logging |

### Identity Services

**Path:** `Identity/`

| Service | Path | Purpose |
|---------|------|---------|
| `ApplicationUser` | `Identity/ApplicationUser.cs` | Extended IdentityUser |
| `TokenService` | `Identity/TokenService.cs` | JWT generation/validation |
| `RefreshTokenService` | `Identity/RefreshTokenService.cs` | Refresh token management |
| `CookieAuthService` | `Identity/CookieAuthService.cs` | Cookie-based auth |

#### Handlers (Co-located with Commands)

**Path:** `Application/Features/{Feature}/Commands/{Action}/` or `Queries/{Action}/`

Handlers are co-located with their Commands/Queries and use constructor injection:
```csharp
// Application/Features/Auth/Commands/Login/LoginCommandHandler.cs
public class LoginCommandHandler
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;

    public LoginCommandHandler(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }

    public async Task<Result<AuthResponse>> Handle(
        LoginCommand cmd,
        CancellationToken ct) { ... }
}
```

### Authorization

**Path:** `Identity/Authorization/`

| Component | Purpose |
|-----------|---------|
| `PermissionHandler` | Permission-based authorization |
| `ResourceAuthorizationHandler` | Resource-level authorization |

### Audit System

**Path:** `Audit/`
**Pattern Doc:** [Hierarchical Audit Logging](backend/patterns/hierarchical-audit-logging.md)

| Component | Purpose |
|-----------|---------|
| `HandlerAuditMiddleware` | CQRS handler execution logging |
| `HttpRequestAuditMiddleware` | HTTP request/response logging |
| `AuditRetentionJob` | Cleanup and archival |

### Services

**Path:** `Services/`
**Pattern Doc:** [DI Auto-Registration](backend/patterns/di-auto-registration.md)

| Service | Interface | Lifetime |
|---------|-----------|----------|
| `EmailService` | `IEmailService` | Scoped |
| `FileStorageService` | `IFileStorage` | Scoped |
| `ImageProcessorService` | `IImageProcessor` | Scoped |
| `DateTimeService` | `IDateTime` | Scoped |
| `CurrentUserService` | `ICurrentUser` | Scoped |
| `JsonDiffService` | `IDiffService` | Scoped |
| `DeviceFingerprintService` | `IDeviceFingerprintService` | Scoped |
| `BackgroundJobsService` | `IBackgroundJobsService` | Scoped |
| `CacheInvalidationService` | `ICacheInvalidationService` | Scoped |

### Media Processing

**Path:** `Media/`

| Component | Purpose |
|-----------|---------|
| `ImageProcessorService` | Main image processor using ImageSharp |
| `ThumbHashGenerator` | Generates ThumbHash blur placeholders |
| `ColorAnalyzer` | Extracts dominant color from images |
| `SrcsetGenerator` | Generates responsive srcset markup |
| `SlugGenerator` | Creates SEO-friendly filenames |

**Processing Pipeline:**
1. Validate image format (JPEG, PNG, GIF, WebP, AVIF, HEIC)
2. Auto-rotate based on EXIF orientation
3. Generate variants in parallel (Thumb 150px, ExtraLarge 1920px)
4. Encode to WebP format
5. Generate ThumbHash for blur placeholder
6. Save to storage and return absolute URLs

**Configuration:** `ImageProcessingSettings` in `appsettings.json`

---

## Web Layer

**Path:** `src/NOIR.Web/`

### API Endpoints

#### Auth Endpoints
**Path:** `Endpoints/AuthEndpoints.cs`
**Prefix:** `/api/auth`

| Method | Route | Handler |
|--------|-------|---------|
| POST | `/login` | `LoginCommand` |
| POST | `/refresh` | `RefreshTokenCommand` |
| POST | `/logout` | `LogoutCommand` |
| GET | `/me` | `GetCurrentUserQuery` |
| PUT | `/profile` | `UpdateUserProfileCommand` |

#### User Endpoints
**Path:** `Endpoints/UserEndpoints.cs`
**Prefix:** `/api/users`

| Method | Route | Handler |
|--------|-------|---------|
| GET | `/` | `GetUsersQuery` |
| POST | `/` | `CreateUserCommand` |
| GET | `/{id}` | `GetUserByIdQuery` |
| PUT | `/{id}` | `UpdateUserCommand` |
| DELETE | `/{id}` | `DeleteUserCommand` |
| GET | `/{id}/roles` | `GetUserRolesQuery` |
| POST | `/{id}/lock` | `LockUserCommand` |
| POST | `/{id}/unlock` | Unlocks user |
| POST | `/{id}/roles` | `AssignRolesToUserCommand` |

#### Role Endpoints
**Path:** `Endpoints/RoleEndpoints.cs`
**Prefix:** `/api/roles`

| Method | Route | Handler |
|--------|-------|---------|
| GET | `/` | `GetRolesQuery` |
| GET | `/{id}` | `GetRoleByIdQuery` |
| POST | `/` | `CreateRoleCommand` |
| PUT | `/{id}` | `UpdateRoleCommand` |
| DELETE | `/{id}` | `DeleteRoleCommand` |
| GET | `/{id}/permissions` | `GetRolePermissionsQuery` |
| GET | `/{id}/effective-permissions` | Gets inherited permissions |
| PUT | `/{id}/permissions` | `AssignPermissionToRoleCommand` |
| DELETE | `/{id}/permissions/{permissionId}` | `RemovePermissionFromRoleCommand` |

#### Permission Endpoints
**Path:** `Endpoints/PermissionEndpoints.cs`
**Prefix:** `/api/permissions`

| Method | Route | Handler |
|--------|-------|---------|
| GET | `/` | `GetAllPermissionsQuery` |
| GET | `/templates` | `GetPermissionTemplatesQuery` |

#### Blog Endpoints
**Path:** `Endpoints/BlogEndpoints.cs`
**Prefix:** `/api/blog`

| Method | Route | Handler |
|--------|-------|---------|
| GET | `/posts` | `GetPostsQuery` |
| GET | `/posts/{id}` | `GetPostQuery` |
| POST | `/posts` | `CreatePostCommand` |
| PUT | `/posts/{id}` | `UpdatePostCommand` |
| DELETE | `/posts/{id}` | `DeletePostCommand` |
| POST | `/posts/{id}/publish` | `PublishPostCommand` |
| GET | `/categories` | `GetCategoriesQuery` |
| POST | `/categories` | `CreateCategoryCommand` |
| PUT | `/categories/{id}` | `UpdateCategoryCommand` |
| DELETE | `/categories/{id}` | `DeleteCategoryCommand` |
| GET | `/tags` | `GetTagsQuery` |
| POST | `/tags` | `CreateTagCommand` |
| PUT | `/tags/{id}` | `UpdateTagCommand` |
| DELETE | `/tags/{id}` | `DeleteTagCommand` |

#### Developer Log Endpoints
**Path:** `Endpoints/DeveloperLogEndpoints.cs`
**Prefix:** `/api/developer-logs`

| Method | Route | Handler |
|--------|-------|---------|
| GET | `/stream` | SignalR hub connection for real-time log streaming |

**Note:** Logs are streamed via SignalR for real-time monitoring.

#### File Endpoints (Static File Serving)
**Path:** `Endpoints/FileEndpoints.cs`
**Prefix:** `/media` (configurable via `Storage:MediaUrlPrefix`)

| Method | Route | Purpose |
|--------|-------|---------|
| GET | `/{*path}` | Serve uploaded media files (avatars, blog images) |

**Allowed Folders:** `avatars/`, `blog/`, `content/`, `images/`
**Caching:** 1 year (immutable, files have unique slugs/GUIDs)

#### Media Endpoints
**Path:** `Endpoints/MediaEndpoints.cs`
**Prefix:** `/api/media`

| Method | Route | Purpose |
|--------|-------|---------|
| POST | `/upload` | Upload and process image with variants |

**Query Parameters:**
- `folder`: Target folder (blog, content, avatars)
- `entityId`: Optional entity ID for avatar storage

**Response:** `MediaUploadResultDto` with absolute URLs, ThumbHash, variants

#### Product Endpoints
**Path:** `Endpoints/ProductEndpoints.cs`
**Prefix:** `/api/products`

| Method | Route | Handler |
|--------|-------|---------|
| GET | `/` | `GetProductsQuery` |
| GET | `/{id}` | `GetProductByIdQuery` |
| POST | `/` | `CreateProductCommand` |
| PUT | `/{id}` | `UpdateProductCommand` |
| DELETE | `/{id}` | `DeleteProductCommand` |
| POST | `/{id}/publish` | `PublishProductCommand` |
| POST | `/{id}/archive` | `ArchiveProductCommand` |
| POST | `/{id}/duplicate` | `DuplicateProductCommand` |
| POST | `/bulk/publish` | `BulkPublishProductsCommand` |
| POST | `/bulk/archive` | `BulkArchiveProductsCommand` |
| POST | `/bulk/delete` | `BulkDeleteProductsCommand` |
| POST | `/bulk/import` | `BulkImportProductsCommand` |
| GET | `/export` | `ExportProductsQuery` |
| GET | `/stats` | `GetProductStatsQuery` |
| POST | `/{id}/variants` | `AddProductVariantCommand` |
| PUT | `/{id}/variants/{variantId}` | `UpdateProductVariantCommand` |
| DELETE | `/{id}/variants/{variantId}` | `DeleteProductVariantCommand` |
| POST | `/{id}/images` | `AddProductImageCommand` |
| POST | `/{id}/images/upload` | `UploadProductImageCommand` |
| PUT | `/{id}/images/{imageId}` | `UpdateProductImageCommand` |
| DELETE | `/{id}/images/{imageId}` | `DeleteProductImageCommand` |
| POST | `/{id}/images/{imageId}/primary` | `SetPrimaryProductImageCommand` |
| POST | `/{id}/images/reorder` | `ReorderProductImagesCommand` |
| POST | `/{id}/options` | `AddProductOptionCommand` |
| PUT | `/{id}/options/{optionId}` | `UpdateProductOptionCommand` |
| DELETE | `/{id}/options/{optionId}` | `DeleteProductOptionCommand` |
| POST | `/{id}/options/{optionId}/values` | `AddProductOptionValueCommand` |
| PUT | `/{id}/options/{optionId}/values/{valueId}` | `UpdateProductOptionValueCommand` |
| DELETE | `/{id}/options/{optionId}/values/{valueId}` | `DeleteProductOptionValueCommand` |

#### Product Category Endpoints
**Path:** `Endpoints/ProductCategoryEndpoints.cs`
**Prefix:** `/api/product-categories`

| Method | Route | Handler |
|--------|-------|---------|
| GET | `/` | `GetProductCategoriesQuery` |
| GET | `/{id}` | `GetProductCategoryByIdQuery` |
| POST | `/` | `CreateProductCategoryCommand` |
| PUT | `/{id}` | `UpdateProductCategoryCommand` |
| DELETE | `/{id}` | `DeleteProductCategoryCommand` |

#### Product Attribute Endpoints
**Path:** `Endpoints/ProductAttributeEndpoints.cs`
**Prefix:** `/api/product-attributes`

| Method | Route | Handler |
|--------|-------|---------|
| GET | `/` | `GetProductAttributesQuery` |
| GET | `/{id}` | `GetProductAttributeByIdQuery` |
| POST | `/` | `CreateProductAttributeCommand` |
| PUT | `/{id}` | `UpdateProductAttributeCommand` |
| DELETE | `/{id}` | `DeleteProductAttributeCommand` |
| POST | `/{id}/values` | `AddProductAttributeValueCommand` |
| PUT | `/{id}/values/{valueId}` | `UpdateProductAttributeValueCommand` |

#### Brand Endpoints
**Path:** `Endpoints/BrandEndpoints.cs`
**Prefix:** `/api/brands`

| Method | Route | Handler |
|--------|-------|---------|
| GET | `/` | `GetBrandsQuery` |
| GET | `/{id}` | `GetBrandByIdQuery` |
| POST | `/` | `CreateBrandCommand` |
| PUT | `/{id}` | `UpdateBrandCommand` |
| DELETE | `/{id}` | `DeleteBrandCommand` |

#### Cart Endpoints
**Path:** `Endpoints/CartEndpoints.cs`
**Prefix:** `/api/cart`

| Method | Route | Handler |
|--------|-------|---------|
| GET | `/` | `GetCartQuery` (AllowAnonymous) |
| GET | `/summary` | `GetCartSummaryQuery` (AllowAnonymous) |
| POST | `/items` | `AddToCartCommand` (AllowAnonymous) |
| PUT | `/items/{itemId}` | `UpdateCartItemCommand` (AllowAnonymous) |
| DELETE | `/items/{itemId}` | `RemoveCartItemCommand` (AllowAnonymous) |
| DELETE | `/` | `ClearCartCommand` (AllowAnonymous) |
| POST | `/merge` | `MergeCartCommand` (RequireAuthorization) |

**Note:** Guest cart uses `noir_cart_session` cookie for session identification.

#### Checkout Endpoints
**Path:** `Endpoints/CheckoutEndpoints.cs`
**Prefix:** `/api/checkout`

| Method | Route | Handler |
|--------|-------|---------|
| GET | `/{sessionId}` | `GetCheckoutSessionQuery` |
| POST | `/initiate` | `InitiateCheckoutCommand` |
| PUT | `/{sessionId}/address` | `SetCheckoutAddressCommand` |
| PUT | `/{sessionId}/shipping` | `SelectShippingMethodCommand` |
| PUT | `/{sessionId}/payment` | `SelectPaymentMethodCommand` |
| POST | `/{sessionId}/complete` | `CompleteCheckoutCommand` |

#### Order Endpoints
**Path:** `Endpoints/OrderEndpoints.cs`
**Prefix:** `/api/orders`

| Method | Route | Handler |
|--------|-------|---------|
| GET | `/` | `GetOrdersQuery` |
| GET | `/{id}` | `GetOrderByIdQuery` |
| POST | `/` | `CreateOrderCommand` |
| POST | `/{id}/confirm` | `ConfirmOrderCommand` |
| POST | `/{id}/ship` | `ShipOrderCommand` |
| POST | `/{id}/deliver` | `DeliverOrderCommand` |
| POST | `/{id}/complete` | `CompleteOrderCommand` |
| POST | `/{id}/cancel` | `CancelOrderCommand` |
| POST | `/{id}/return` | `ReturnOrderCommand` |

#### Shipping Endpoints
**Path:** `Endpoints/ShippingEndpoints.cs`, `Endpoints/ShippingProviderEndpoints.cs`
**Prefix:** `/api/shipping`, `/api/shipping-providers`

| Method | Route | Handler |
|--------|-------|---------|
| GET | `/providers` | `GetShippingProvidersQuery` |
| GET | `/providers/{id}` | Provider details |
| POST | `/providers` | `CreateShippingProviderCommand` |
| PUT | `/providers/{id}` | `UpdateShippingProviderCommand` |
| DELETE | `/providers/{id}` | `DeleteShippingProviderCommand` |
| POST | `/orders` | `CreateShippingOrderCommand` |
| GET | `/orders` | `GetShippingOrdersQuery` |
| POST | `/rates/calculate` | `CalculateShippingRatesQuery` |

#### Inventory Endpoints
**Path:** `Endpoints/InventoryEndpoints.cs`
**Prefix:** `/api/inventory`

| Method | Route | Handler |
|--------|-------|---------|
| GET | `/products/{productId}/variants/{variantId}/history` | `GetStockHistoryQuery` |
| POST | `/movements` | `CreateStockMovementCommand` |
| GET | `/receipts` | `GetInventoryReceiptsQuery` |
| GET | `/receipts/{id}` | `GetInventoryReceiptByIdQuery` |
| POST | `/receipts` | `CreateInventoryReceiptCommand` |
| POST | `/receipts/{id}/confirm` | `ConfirmInventoryReceiptCommand` |
| POST | `/receipts/{id}/cancel` | `CancelInventoryReceiptCommand` |

**Permissions:** `OrdersRead` for GET endpoints, `OrdersManage` for POST endpoints.

#### Payment Endpoints
**Path:** `Endpoints/PaymentEndpoints.cs`
**Prefix:** `/api/payments`

| Method | Route | Handler |
|--------|-------|---------|
| GET | `/transactions` | `GetPaymentTransactionsQuery` |
| GET | `/transactions/{id}` | `GetPaymentTransactionQuery` |
| POST | `/` | `CreatePaymentCommand` |
| POST | `/{id}/cancel` | `CancelPaymentCommand` |
| GET | `/gateways` | `GetPaymentGatewaysQuery` |
| GET | `/gateways/{id}` | `GetPaymentGatewayQuery` |
| GET | `/gateways/active` | `GetActiveGatewaysQuery` |
| POST | `/gateways` | `ConfigureGatewayCommand` |
| PUT | `/gateways/{id}` | `UpdateGatewayCommand` |
| POST | `/webhook` | `ProcessWebhookCommand` |
| GET | `/refunds` | `GetRefundsQuery` |
| POST | `/refunds/request` | `RequestRefundCommand` |
| POST | `/refunds/{id}/approve` | `ApproveRefundCommand` |
| POST | `/refunds/{id}/reject` | `RejectRefundCommand` |
| GET | `/cod/pending` | `GetPendingCodPaymentsQuery` |
| POST | `/cod/{id}/confirm` | `ConfirmCodCollectionCommand` |
| GET | `/webhook-logs` | `GetWebhookLogsQuery` |

#### Product Filter Endpoints
**Path:** `Endpoints/ProductFilterEndpoints.cs`
**Prefix:** `/api/product-filters`

| Method | Route | Handler |
|--------|-------|---------|
| POST | `/filter` | `FilterProductsQuery` |
| GET | `/categories/{slug}/filters` | `GetCategoryFiltersQuery` |

#### Filter Analytics Endpoints
**Path:** `Endpoints/FilterAnalyticsEndpoints.cs`
**Prefix:** `/api/filter-analytics`

| Method | Route | Handler |
|--------|-------|---------|
| POST | `/events` | `CreateFilterEventCommand` |
| GET | `/popular` | `GetPopularFiltersQuery` |

#### Legal Page Endpoints
**Path:** `Endpoints/LegalPageEndpoints.cs`
**Prefix:** `/api/legal-pages`

| Method | Route | Handler |
|--------|-------|---------|
| GET | `/` | `GetLegalPagesQuery` |
| GET | `/{id}` | `GetLegalPageQuery` |
| PUT | `/{id}` | `UpdateLegalPageCommand` |
| POST | `/{id}/revert` | `RevertLegalPageToDefaultCommand` |

#### Public Legal Page Endpoints
**Path:** `Endpoints/PublicLegalPageEndpoints.cs`
**Prefix:** `/api/public/legal-pages`

| Method | Route | Handler |
|--------|-------|---------|
| GET | `/{slug}` | `GetPublicLegalPageQuery` (AllowAnonymous) |

#### Platform Settings Endpoints
**Path:** `Endpoints/PlatformSettingsEndpoints.cs`
**Prefix:** `/api/platform-settings`

| Method | Route | Handler |
|--------|-------|---------|
| GET | `/smtp` | `GetSmtpSettingsQuery` |
| PUT | `/smtp` | `UpdateSmtpSettingsCommand` |
| POST | `/smtp/test` | `TestSmtpConnectionCommand` |

#### Tenant Settings Endpoints
**Path:** `Endpoints/TenantSettingsEndpoints.cs`
**Prefix:** `/api/tenant-settings`

| Method | Route | Handler |
|--------|-------|---------|
| GET | `/branding` | `GetBrandingSettingsQuery` |
| PUT | `/branding` | `UpdateBrandingSettingsCommand` |
| GET | `/contact` | `GetContactSettingsQuery` |
| PUT | `/contact` | `UpdateContactSettingsCommand` |
| GET | `/regional` | `GetRegionalSettingsQuery` |
| PUT | `/regional` | `UpdateRegionalSettingsCommand` |
| GET | `/smtp` | `GetTenantSmtpSettingsQuery` |
| PUT | `/smtp` | `UpdateTenantSmtpSettingsCommand` |
| POST | `/smtp/test` | `TestTenantSmtpConnectionCommand` |
| POST | `/smtp/revert` | `RevertTenantSmtpSettingsCommand` |

#### Dashboard Endpoints
**Path:** `Endpoints/DashboardEndpoints.cs`
**Prefix:** `/api/dashboard`

| Method | Route | Handler |
|--------|-------|---------|
| GET | `/metrics` | `GetDashboardMetricsQuery` |

**Query Parameters:** `topProducts` (default 5), `lowStockThreshold` (default 10), `recentOrders` (default 10), `salesDays` (default 30)
**Permission:** `OrdersRead`

### Middleware

**Path:** `Middleware/`

| Middleware | Purpose |
|------------|---------|
| `ExceptionHandlingMiddleware` | Global exception handling, ProblemDetails |
| `SecurityHeadersMiddleware` | Security headers (CSP, HSTS, etc.) |

### Frontend

**Path:** `frontend/`
**Docs:** [Frontend Architecture](frontend/architecture.md)

| Directory | Purpose |
|-----------|---------|
| `src/components/` | Reusable UI components (shadcn/ui) |
| `src/portal-app/` | Domain-driven feature modules |
| `src/hooks/` | Custom React hooks (usePermissions, etc.) |
| `src/services/` | API client and services |
| `src/contexts/` | React contexts (Auth, Theme, Notification) |
| `src/i18n/` | Internationalization |
| `src/lib/` | Utilities |
| `src/types/` | TypeScript types (auto-generated) |

#### Custom UI Components

| Component | Path | Usage |
|-----------|------|-------|
| `EmptyState` | `uikit/empty-state/` | Tables with no data |
| `Pagination` | `uikit/pagination/` | Data table pagination |
| `ColorPicker` | `uikit/color-picker/` | Role color selection |
| `TippyTooltip` | `uikit/tippy-tooltip/` | Rich tooltips with headers |
| `DateRangePicker` | `uikit/date-range-picker/` | Date range selection |
| `ThemeToggle` | `uikit/theme-toggle/` | Segmented Light/Dark toggle with animated indicator |
| `ThemeToggleCompact` | `uikit/theme-toggle/` | Compact icon button for theme switching |

#### Permission-Based UI Rendering

The frontend uses `usePermissions` hook to conditionally render UI elements based on user permissions:

```tsx
import { usePermissions, Permissions } from '@/hooks/usePermissions'

function UserActions() {
  const { hasPermission } = usePermissions()
  const canEdit = hasPermission(Permissions.UsersUpdate)
  const canDelete = hasPermission(Permissions.UsersDelete)

  return (
    <>
      {canEdit && <Button onClick={handleEdit}>Edit</Button>}
      {canDelete && <Button onClick={handleDelete}>Delete</Button>}
    </>
  )
}
```

**Key components using permission-based rendering:**
- `UsersPage` - Create, Edit, Delete, Assign Roles buttons
- `UserTable` - Action menu items per permission
- `EmailTemplatesPage` - Edit button visibility
- `RolesPage` - CRUD actions based on role permissions

#### Cross-Component Communication (Profile Changes)

When user profile data changes (avatar, email, name), other components like Sidebar need to refresh. Use the `avatar-updated` custom event pattern:

```tsx
// ProfileForm.tsx - Dispatch event after profile changes
const notifyProfileChanged = () => window.dispatchEvent(new Event('avatar-updated'))

// After any profile update:
await refreshUser()
notifyProfileChanged()
toast.success('Profile updated')
```

```tsx
// Sidebar.tsx - Listen for profile changes
useEffect(() => {
  const handleAvatarUpdate = () => { checkAuth() }
  window.addEventListener('avatar-updated', handleAvatarUpdate)
  return () => window.removeEventListener('avatar-updated', handleAvatarUpdate)
}, [checkAuth])
```

**When to dispatch `avatar-updated`:**
- Avatar upload/remove
- Email change (avatar color is email-based via `getAvatarColor()`)
- Profile name change (Sidebar shows displayName)

#### API Error Handling

The `apiClient.ts` provides user-friendly error messages for HTTP status codes:
- **403 Forbidden**: Shows "You don't have permission to perform this action." (i18n: `messages.permissionDenied`)
- **401 Unauthorized**: Shows "Your session has expired. Please sign in again." (i18n: `messages.sessionExpired`)

#### Form Validation Standards

**Standard Pattern:** All forms MUST use `react-hook-form` + Zod + shadcn/ui Form components with `mode: 'onBlur'`.

**Why `onBlur`:**
- Validates after user finishes typing (better UX)
- Immediate feedback before form submission
- Less intrusive than `onChange`
- Consistent behavior across all forms

**Required Pattern:**

```tsx
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import * as z from 'zod'
import { Form, FormField, FormItem, FormLabel, FormControl, FormMessage } from '@uikit'

// 1. Define Zod schema
const formSchema = z.object({
  email: z.string().min(1, 'Email is required').email('Invalid email'),
})

// 2. Initialize form with mode: 'onBlur'
const form = useForm<z.infer<typeof formSchema>>({
  resolver: zodResolver(formSchema),
  mode: 'onBlur',  // REQUIRED
  defaultValues: { email: '' },
})

// 3. Use FormField components
<Form {...form}>
  <form onSubmit={form.handleSubmit(onSubmit)} noValidate>
    <FormField
      control={form.control}
      name="email"
      render={({ field }) => (
        <FormItem>
          <FormLabel>Email *</FormLabel>
          <FormControl>
            <Input type="text" {...field} />
          </FormControl>
          <FormMessage /> {/* Auto-displays errors */}
        </FormItem>
      )}
    />
  </form>
</Form>
```

**Key Benefits:**
- `FormLabel` auto-turns red on error
- `FormMessage` auto-displays validation errors
- Type safety from Zod schema
- Consistent validation timing

**Anti-Pattern (DEPRECATED):** Manual `useState` for errors/touched state.

**Hook:** Use `useValidatedForm` for complex forms (defaults to `mode: 'onBlur'`).

**Error Message Styling:** All error messages use `text-sm font-medium text-destructive` for consistency with `FormMessage`.

**Full Documentation:** [Frontend Architecture - Form Validation Standards](frontend/architecture.md#form-validation-standards)

---

## Cross-Cutting Concerns

### Authorization

**Location:** `Infrastructure/Identity/Authorization/`

NOIR uses a hybrid authorization model:
- **Role-based**: Traditional roles (Admin, User, etc.)
- **Permission-based**: Fine-grained `resource:action:scope` permissions

Permission format: `{resource}:{action}:{scope}`
- Example: `users:create:all`, `orders:read:own`

### Audit Trail

**Docs:** [Hierarchical Audit Logging](backend/patterns/hierarchical-audit-logging.md)

Three-level audit system:
1. **HTTP Level** - Request/response logging
2. **Handler Level** - Command/query execution
3. **Entity Level** - Database changes (before/after diff)

### Multi-Tenancy

**Package:** Finbuckle.MultiTenant 10.0.2
**Interceptor:** `TenantIdSetterInterceptor`

#### Tenant Entity Pattern

The `Tenant` entity inherits from Finbuckle's `TenantInfo` class and uses **immutable factory methods** due to `TenantInfo` having `required init`-only properties (Finbuckle 10.x breaking change):

```csharp
// Create new tenant
var tenant = Tenant.Create("acme", "Acme Corp");

// Update (returns new instance)
var updated = tenant.CreateUpdated("acme", "Updated Name", ...);

// Soft delete (returns new instance)
var deleted = tenant.CreateDeleted("admin-user-id");
```

**Important:** Cannot use `with` expressions on `Tenant` (not a record). Use factory methods instead.

NOIR implements a multi-tenant architecture where:
- Users can belong to **multiple tenants** via `UserTenantMembership`
- Each membership has a **role** (Owner, Admin, Member, Viewer)
- One membership can be marked as **default** for the user
- Tenant-specific entities automatically get `TenantId` set via interceptor

#### User-Tenant Membership Model

```csharp
// UserTenantMembership - Platform-level entity (NOT tenant-scoped)
public class UserTenantMembership : Entity<Guid>
{
    public Guid UserId { get; }         // User reference
    public Guid TenantId { get; }       // Tenant reference
    public TenantRole Role { get; }     // Owner, Admin, Member, Viewer
    public bool IsDefault { get; }      // User's default tenant
    public DateTimeOffset JoinedAt { get; }
}
```

#### Tenant Roles

| Role | Permissions |
|------|-------------|
| Owner | Full control, can delete tenant |
| Admin | Manage users and settings |
| Member | Standard access |
| Viewer | Read-only access |

#### Platform-Level vs Tenant-Level Data

NOIR supports a **fallback pattern** where data can exist at two levels:

| Level | TenantId Value | Scope | Example |
|-------|----------------|-------|---------|
| Platform | `null` | Shared across all tenants | Default email templates |
| Tenant | `Guid` | Specific to one tenant | Custom email templates |

**Query Pattern for Fallback:**
```csharp
// First check tenant-specific, then fallback to platform
var templates = await _dbContext.Set<EmailTemplate>()
    .IgnoreQueryFilters()  // Bypass tenant filter
    .Where(t => t.Name == templateName && t.IsActive && !t.IsDeleted)
    .ToListAsync();

// Prefer tenant-specific, fallback to platform (TenantId = null)
var template = templates.FirstOrDefault(t => t.TenantId == currentTenantId)
    ?? templates.FirstOrDefault(t => t.TenantId == null);
```

**Key Points:**
- Platform-level data serves as **defaults** for all tenants
- Tenants can **override** platform defaults with their own data
- Use `IgnoreQueryFilters()` when querying both levels
- Seeder creates platform-level data with `TenantId = null`

### Validation

**Package:** FluentValidation
**Location:** Each command has a corresponding `*Validator.cs` file

```csharp
public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).MinimumLength(6);
    }
}
```

---

## Development Guide

### Common Tasks

| Task | Files to Modify | Pattern Doc |
|------|-----------------|-------------|
| Add Entity | `Domain/Entities/`, `Infrastructure/Persistence/Configurations/` | [Entity Configuration](backend/patterns/entity-configuration.md) |
| Add Command | `Application/Features/<Feature>/Commands/` | Feature structure |
| Add Query | `Application/Features/<Feature>/Queries/` | Feature structure |
| Add Handler | `Infrastructure/Identity/Handlers/` | CQRS pattern |
| Add Endpoint | `Web/Endpoints/` | Minimal API |
| Add Specification | `Application/Specifications/` | [Repository & Specification](backend/patterns/repository-specification.md) |
| Add Service | Add marker interface (`IScopedService`, etc.) | [DI Auto-Registration](backend/patterns/di-auto-registration.md) |

### Critical Rules

1. **Use Specifications** for all database queries - never raw `DbSet`
2. **Tag all specifications** with `TagWith("MethodName")` for SQL debugging
3. **Use IUnitOfWork** for persistence - repository methods do NOT auto-save
4. **Use AsTracking** for mutation specs - default is `AsNoTracking`
5. **Co-locate Command + Handler + Validator** - all in `Application/Features/{Feature}/Commands/{Action}/`
6. **Soft delete only** - never hard delete (except GDPR)
7. **Marker interfaces** for DI auto-registration
8. **No using statements** in files - add to `GlobalUsings.cs`
9. **Audit logging for user actions** - Commands that create/update/delete via frontend MUST implement `IAuditableCommand`. Requires: (a) Command implements `IAuditableCommand<TResult>`, (b) Endpoint sets `UserId`, (c) Frontend calls `usePageContext()`. See [Audit Logging](backend/patterns/hierarchical-audit-logging.md)
10. **No console.error in frontend** - Errors are visible in browser Network tab; use toast notifications for user feedback instead
11. **Correct Error factory method usage** - See [Error Factory Methods](#error-factory-methods) for correct parameter order

### Performance Rules

| Scenario | Use |
|----------|-----|
| Read-only queries | `AsNoTracking` (default) |
| Multiple collections | `.AsSplitQuery()` |
| Bulk operations (1000+) | [Bulk extension methods](backend/patterns/bulk-operations.md) |

### Error Factory Methods

**Location:** `Domain/Common/Result.cs`

The `Error` record provides factory methods for creating typed errors. **Be careful with parameter order** - incorrect usage causes error codes to display instead of user-friendly messages!

| Method | Signature | Use Case |
|--------|-----------|----------|
| `Validation` | `(propertyName, message, code?)` | Field-specific validation errors |
| `NotFound` | `(message, code?)` | Resource not found |
| `Conflict` | `(message, code?)` | Resource conflicts |
| `Unauthorized` | `(message?, code?)` | Authentication required |
| `Forbidden` | `(message?, code?)` | Permission denied |
| `TooManyRequests` | `(message?, code?)` | Rate limiting |
| `Failure` | `(code, message)` | Generic failures |

**CRITICAL: `Error.Validation` requires `propertyName` as first parameter!**

```csharp
// ✅ CORRECT - property name first, then message, then code
Error.Validation("newEmail", "This email address is already in use.", ErrorCodes.Auth.DuplicateEmail)

// ❌ WRONG - missing property name causes error code to become the message!
Error.Validation("This email address is already in use.", ErrorCodes.Auth.DuplicateEmail)
// Results in: Code=NOIR-VAL-0001, Message="NOIR-AUTH-1011" (swapped!)
```

**For errors without a specific field**, use the Error constructor directly:
```csharp
new Error(ErrorCodes.Auth.DuplicateEmail, "This email address is already in use.", ErrorType.Validation)
```

---

## Testing

**Path:** `tests/`

### Test Projects

| Project | Tests | Purpose |
|---------|-------|---------|
| `NOIR.Domain.UnitTests` | 842 | Domain entity tests |
| `NOIR.Application.UnitTests` | 5,231 | Handler, specification, validator tests |
| `NOIR.ArchitectureTests` | 25 | Dependency constraints |
| `NOIR.IntegrationTests` | 654 | API integration tests |
| **Total** | **6,750+** | |

### Test Patterns

```csharp
// Handler test
[Fact]
public async Task Handle_ValidCommand_ReturnsSuccess()
{
    // Arrange
    var command = new CreateUserCommand("test@example.com", "password");

    // Act
    var result = await CreateUserHandler.Handle(command, _mockRepo.Object, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();
}
```

### Running Tests

```bash
# All tests
dotnet test src/NOIR.sln

# Specific project
dotnet test tests/NOIR.Domain.UnitTests

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

---

## Documentation Map

### Core Documentation

| Document | Path | Description |
|----------|------|-------------|
| **KNOWLEDGE_BASE** | `docs/KNOWLEDGE_BASE.md` | This document - complete codebase reference |
| **API_INDEX** | `docs/API_INDEX.md` | Complete REST API endpoint documentation |
| **ARCHITECTURE** | `docs/ARCHITECTURE.md` | Architecture overview, patterns, decisions |
| **PROJECT_INDEX** | `docs/PROJECT_INDEX.md` | Project structure and quick reference |

### Backend Documentation

| Document | Path | Description |
|----------|------|-------------|
| Backend README | `docs/backend/README.md` | Overview and quick start |
| Repository & Specification | `docs/backend/patterns/repository-specification.md` | Data access patterns |
| DI Auto-Registration | `docs/backend/patterns/di-auto-registration.md` | Service registration |
| Entity Configuration | `docs/backend/patterns/entity-configuration.md` | EF Core setup |
| JWT Refresh Tokens | `docs/backend/patterns/jwt-refresh-token.md` | Token rotation |
| Audit Logging | `docs/backend/patterns/hierarchical-audit-logging.md` | 3-level audit |
| Bulk Operations | `docs/backend/patterns/bulk-operations.md` | High-volume data |
| Before State Resolver | `docs/backend/patterns/before-state-resolver.md` | Audit before-state pattern |
| JSON Enum Serialization | `docs/backend/patterns/json-enum-serialization.md` | Enum string serialization |
| Technical Checklist | `docs/backend/patterns/technical-checklist.md` | Implementation checklist |

### Research Documents

| Document | Path | Description |
|----------|------|-------------|
| Audit Logging Comparison | `docs/backend/research/hierarchical-audit-logging-comparison-2025.md` | Technology comparison |
| Role & Permission Systems | `docs/backend/research/role-permission-system-research.md` | RBAC/ReBAC patterns (Consolidated) |
| Cache Busting | `docs/backend/research/cache-busting-best-practices.md` | Frontend cache strategies |
| E-commerce UX Patterns | `docs/backend/research/ecommerce-ux-patterns-2026.md` | E-commerce UX best practices |
| SEO Best Practices | `docs/backend/research/seo-meta-and-hint-text-best-practices.md` | SEO meta and hint text |
| Validation Unification | `docs/backend/research/validation-unification-plan.md` | Unified validation strategy |
| Vietnam Shipping | `docs/backend/research/vietnam-shipping-integration-2026.md` | Vietnam shipping providers |

### Architecture Notes

| Document | Path | Description |
|----------|------|-------------|
| Tenant ID Interceptor | `docs/backend/architecture/tenant-id-interceptor.md` | Multi-tenancy interceptor pattern |

### Frontend Documentation

| Document | Path | Description |
|----------|------|-------------|
| Frontend README | `docs/frontend/README.md` | Overview and setup |
| Architecture | `docs/frontend/architecture.md` | Project structure |
| API Types | `docs/frontend/api-types.md` | Type generation |
| Localization | `docs/frontend/localization-guide.md` | i18n setup |
| Color Schema | `docs/frontend/COLOR_SCHEMA_GUIDE.md` | Color guidelines |
| Vibe Kanban Integration | `docs/frontend/vibe-kanban-integration.md` | Task management integration |

### Frontend Designs

| Document | Path | Description |
|----------|------|-------------|
| Notification Dropdown | `docs/frontend/designs/notification-dropdown-ui-design.md` | Notification bell design |

### Architecture Decisions

| ADR | Path | Description |
|-----|------|-------------|
| 001 | `docs/decisions/001-tech-stack.md` | Technology selection |
| 002 | `docs/decisions/002-frontend-ui-stack.md` | Frontend UI choices |
| 003 | `docs/decisions/003-vertical-slice-cqrs.md` | Vertical slice architecture for CQRS |

### Project Plans

| Document | Path | Description |
|----------|------|-------------|
| Feature Roadmap 2026 | `docs/plans/feature-roadmap-2026.md` | Feature planning |

### AI Instructions

| Document | Path | Description |
|----------|------|-------------|
| CLAUDE.md | `CLAUDE.md` | Claude Code specific instructions |
| AGENTS.md | `AGENTS.md` | Universal AI agent guidelines |

---

## Quick Reference

### Commands

```bash
# Build & Run
dotnet build src/NOIR.sln
dotnet run --project src/NOIR.Web
dotnet watch --project src/NOIR.Web

# Tests
dotnet test src/NOIR.sln

# Frontend
cd src/NOIR.Web/frontend
pnpm install && pnpm run dev
pnpm run generate:api  # Sync types from backend

# Database
dotnet ef migrations add NAME --project src/NOIR.Infrastructure --startup-project src/NOIR.Web

# Docker
docker-compose up -d  # Start SQL Server + MailHog
```

### URLs

| URL | Purpose |
|-----|---------|
| `http://localhost:3000` | Application (frontend + API via proxy) |
| `http://localhost:3000/api/docs` | API documentation (Scalar) |
| `http://localhost:3000/hangfire` | Background jobs dashboard |
| `http://localhost:4000` | Backend only (production-like) |

### Default Credentials

- **Email:** `admin@noir.local`
- **Password:** `123qwe`

---

*Updated: 2026-02-18 | Total Tests: 6,750+ | Features: 26 | Endpoints: 200+ | Entities: 50+*
