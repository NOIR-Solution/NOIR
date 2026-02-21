# Feature Management System - Requirements Specification

> Date: 2026-02-21
> Status: Draft (Pending user approval)
> Research: [module-feature-management-systems.md](module-feature-management-systems.md)

---

## 1. Goals & Motivation

### Problem
NOIR currently has 33 feature modules all compiled and always-on. The only gating mechanism is user-level permissions. There is no way for a tenant admin to turn off entire modules (e.g., Blog, E-commerce) or for the platform admin to restrict which modules a tenant can use.

### Goals
1. **Module-level toggling**: Platform admin and tenant admin can enable/disable entire modules per tenant
2. **Feature-level toggling**: Individual features within modules can be independently toggled (respecting parent module state)
3. **Infrastructure awareness**: Seeders, background jobs, and configs can easily check if a module/feature is enabled
4. **Code-defined catalog**: Modules and features are declared in C# for type safety and discoverability
5. **Data preservation**: Disabling a module hides UI and blocks API but preserves existing data

### Non-Goals (Out of Scope)
- Subscription plan/tier system (design to allow future addition, but not implement now)
- A/B testing or percentage rollout
- Per-user feature flags (use existing permission system for that)
- Runtime dynamic feature definitions (all features defined in code)

---

## 2. Definitions

| Term | Definition |
|------|-----------|
| **Module** | A logical group of related features (e.g., "Blog", "Ecommerce.Orders"). Code-defined. |
| **Feature** | An individual toggleable capability within a module (e.g., "Blog.Comments", "Ecommerce.Reviews"). Code-defined. |
| **Module Catalog** | The complete registry of all modules and features, defined in code via `IModuleDefinition`. |
| **Platform Availability** | Whether a module/feature is AVAILABLE to a tenant (set by platform admin). Hard limit. |
| **Tenant Toggle** | Whether a tenant admin has ENABLED an available module/feature. Soft toggle. |
| **Effective State** | The resolved state: `enabled` only if BOTH platform-available AND tenant-enabled. |

---

## 3. Functional Requirements

### FR-1: Module & Feature Catalog (Code-Defined)

**FR-1.1** Each module is declared as a C# class implementing `IModuleDefinition`.
- Properties: `Name` (unique key), `DisplayName`, `Description`, `Icon`, `SortOrder`
- Contains a list of `FeatureDefinition` children
- Registered as `ISingletonService` for auto-discovery via Scrutor

**FR-1.2** Each feature is defined within its parent module.
- Properties: `Name` (unique key, format: `ModuleName.FeatureName`), `DisplayName`, `Description`, `DefaultEnabled` (bool)
- `DefaultEnabled = true` means the feature is ON by default for all tenants (opt-out model)

**FR-1.3** A `IModuleCatalog` service aggregates all `IModuleDefinition` instances.
- Provides: `GetAllModules()`, `GetModule(name)`, `GetFeature(name)`, `GetFeaturesForModule(moduleName)`
- Singleton, initialized once at startup

**FR-1.4** Initial implementation defines all 33 existing feature modules:
- **Core** (always-on, cannot be disabled): Auth, Users, Roles, Permissions, Dashboard, Settings, Audit, DeveloperLogs, Notifications
- **E-commerce**: Products, Categories, Brands, ProductAttributes, ProductFilter, ProductFilterIndex, Cart, Checkout, Orders, Payments, Inventory, Promotions, Reviews, Customers, CustomerGroups, Wishlist
- **Content**: Blog, BlogCategories, BlogTags
- **Platform**: Tenants, EmailTemplates, LegalPages
- **Analytics**: Dashboard (always-on), Reports

### FR-2: Two-Layer Override Model

**FR-2.1** **Platform Availability** (set by platform admin):
- Per-tenant whitelist of available modules/features
- Stored in DB as `TenantModuleAvailability` records
- Default: All modules are available to all tenants (opt-out)
- Platform admin can mark a module as "unavailable" for a specific tenant
- When unavailable: tenant admin cannot see or toggle the module

**FR-2.2** **Tenant Toggle** (set by tenant admin):
- Per-tenant enable/disable of available modules/features
- Stored in DB as `TenantModuleToggle` records
- Default: All available modules are enabled (opt-out)
- Tenant admin can disable a module they don't need

**FR-2.3** **Effective State Resolution**:
```
Effective = PlatformAvailable AND TenantEnabled AND ParentModuleEffective
```
- If platform marks module unavailable → always OFF regardless of tenant toggle
- If tenant disables module → OFF even though platform allows it
- If parent module is OFF → all child features are OFF regardless of their individual state

### FR-3: Strict Hierarchy

**FR-3.1** Disabling a module automatically disables ALL child features.
- Child feature toggles are preserved in DB but overridden at resolution time
- Re-enabling the module restores previous child feature states

**FR-3.2** Cannot enable a child feature if parent module is disabled.
- API returns validation error
- UI disables child toggles with visual indicator (grayed out)

**FR-3.3** Core modules (Auth, Users, Roles, etc.) cannot be disabled.
- `IModuleDefinition.IsCore = true` flag
- API and UI prevent toggling core modules

### FR-4: API Gating

**FR-4.1** When a module/feature is effectively disabled for the current tenant:
- API endpoints return **403 Forbidden** with body:
  ```json
  { "title": "Feature Not Available", "detail": "The feature 'Blog' is not enabled for your organization.", "status": 403 }
  ```
- NOT 404 (preserves discoverability for debugging)

**FR-4.2** Gating is implemented via:
- **MediatR Pipeline Behavior** (`FeatureCheckBehavior<TRequest, TResponse>`) for commands/queries decorated with `[RequiresFeature("...")]`
- **Minimal API Endpoint Filter** (`.RequireFeature("...")`) for endpoint-level gating
- Both use `IFeatureChecker.IsEnabledAsync(featureName)` internally

**FR-4.3** `[RequiresFeature]` attribute can be placed on:
- Command/Query classes (checked in MediatR pipeline)
- Endpoint registrations (checked in endpoint filter)
- Multiple features: `[RequiresFeature("Ecommerce", "Ecommerce.Orders")]` (all must be enabled)

### FR-5: Frontend Gating

**FR-5.1** Backend provides a `GET /api/features/current-tenant` endpoint that returns:
```json
{
  "modules": {
    "Blog": { "enabled": true, "available": true },
    "Ecommerce": { "enabled": true, "available": true },
    "Ecommerce.Reviews": { "enabled": false, "available": true }
  }
}
```

**FR-5.2** React hook `useFeature(name: string): { enabled: boolean; available: boolean }` for component-level checks.

**FR-5.3** React hook `useModule(name: string): boolean` for simple module-level checks.

**FR-5.4** Navigation (Sidebar.tsx): Items for disabled modules are **hidden** (not grayed out).
- Each `navSection` / `navItem` is associated with a module name
- Items filtered by both permission AND feature check

**FR-5.5** Route-level protection: If user navigates directly to a disabled module's URL, show a "Module not available" page (not 404).

### FR-6: Background Job Integration

**FR-6.1** Background jobs that are module-specific MUST check feature state before executing per-tenant work.
- Pattern: Iterate active tenants → check `IFeatureChecker.IsEnabledAsync` → skip if disabled
- Example: `ProductFilterIndexMaintenanceJob` checks `Ecommerce.ProductFilter` before processing

**FR-6.2** Provide a helper `ITenantJobRunner` service:
```csharp
await _tenantJobRunner.RunForEnabledTenantsAsync("Ecommerce.ProductFilter", async (tenantId, scope) => {
    // Execute job logic for this tenant
}, ct);
```

### FR-7: Seeder Integration

**FR-7.1** Seeders for non-core modules check feature state before seeding.
- Use `IFeatureChecker` in `SeedAsync` to skip seeding for disabled modules
- Example: Blog seeders skip if Blog module is disabled for the current tenant

**FR-7.2** Platform-level seeders (TenantId = null) are NOT gated by features.
- Feature management seeds (module catalog defaults) always run

### FR-8: Admin UI

**FR-8.1** Add "Modules & Features" section to existing Tenant Settings page.
- Visible to both platform admin and tenant admin (with different capabilities)

**FR-8.2** **Platform Admin view** (when managing a specific tenant):
- Tree view of all modules with features as children
- Toggle switch for "Available" (platform availability)
- Toggle switch for "Enabled" (tenant toggle) - can set on behalf of tenant
- Core modules shown as always-on (no toggle)
- Visual indicator for effective state (green = on, gray = off, red = platform-blocked)

**FR-8.3** **Tenant Admin view**:
- Tree view of AVAILABLE modules only (platform-blocked modules hidden)
- Toggle switch for "Enabled" only
- Child features shown under parent module, disabled if parent is OFF
- Core modules shown as always-on

**FR-8.4** Changes take effect immediately (no page reload required).
- API call → cache invalidation → SignalR notification to connected clients
- Frontend refreshes feature state via SignalR event handler

### FR-9: Caching

**FR-9.1** **Cross-request cache** (FusionCache, already in NOIR):
- Cache key: `features:tenant:{tenantId}` → Dictionary of all features with effective state
- TTL: 5 minutes (configurable)
- Explicit invalidation when platform admin or tenant admin changes toggles

**FR-9.2** **Per-request cache**:
- First `IFeatureChecker.IsEnabledAsync` call in a request loads all tenant features into a request-scoped dictionary
- Subsequent calls in same request are O(1) dictionary lookups
- No DB hit after first call within a request

**FR-9.3** Cache invalidation triggers:
- Platform admin changes module availability → invalidate for affected tenant
- Tenant admin toggles module → invalidate for own tenant
- SignalR notification to all connected clients of affected tenant

---

## 4. Non-Functional Requirements

### NFR-1: Performance
- Feature check must complete in < 1ms after initial cache load
- Initial cache load (cold) must complete in < 50ms
- No additional DB queries per request after cache is warm

### NFR-2: Data Model
- All unique constraints include TenantId (NOIR Rule 18)
- Soft delete on all feature management entities (NOIR Rule 4)
- Audit trail via IAuditableEntity on toggle changes

### NFR-3: Backward Compatibility
- Existing permissions continue to work unchanged
- Feature checks are additive (AND with existing permission checks)
- No existing API contract changes; new endpoints only
- No breaking changes to existing frontend components

### NFR-4: Testing
- Unit tests for resolution logic (effective state calculation)
- Unit tests for MediatR pipeline behavior
- Unit tests for each module definition (all 33 modules registered)
- Integration tests for cache invalidation
- Architecture test: every non-core command/query has `[RequiresFeature]`

---

## 5. User Stories

### US-1: Platform Admin Manages Module Availability
**As a** platform admin
**I want to** set which modules are available for each tenant
**So that** I can control what features tenants can access

**Acceptance Criteria:**
- [ ] Can view all modules/features for any tenant
- [ ] Can toggle module availability per tenant
- [ ] Unavailable modules are hidden from tenant admin's settings
- [ ] Disabling availability for a module also blocks all its child features
- [ ] Core modules cannot be made unavailable
- [ ] Changes take effect immediately

### US-2: Tenant Admin Toggles Modules
**As a** tenant admin
**I want to** enable/disable modules for my organization
**So that** I can simplify my workspace by hiding features we don't use

**Acceptance Criteria:**
- [ ] Can view only modules that platform admin has made available
- [ ] Can toggle modules on/off
- [ ] Disabling a module hides all related navigation items
- [ ] Disabling a module returns 403 on related API endpoints
- [ ] Data is preserved when a module is disabled
- [ ] Re-enabling a module restores access to existing data
- [ ] Child features are automatically disabled when parent module is off
- [ ] Cannot enable child features when parent module is off

### US-3: Developer Adds Feature Gating to New Command
**As a** developer
**I want to** gate a command behind a feature flag with a simple attribute
**So that** the command is automatically blocked when the feature is disabled

**Acceptance Criteria:**
- [ ] Adding `[RequiresFeature("Module.Feature")]` to a command class is sufficient
- [ ] MediatR pipeline automatically checks feature state
- [ ] Returns standardized error when feature is disabled
- [ ] Works with both commands and queries

### US-4: Background Job Respects Feature State
**As a** developer
**I want to** background jobs to skip tenants where the relevant module is disabled
**So that** we don't waste resources on disabled features

**Acceptance Criteria:**
- [ ] `ITenantJobRunner` helper iterates only enabled tenants
- [ ] Feature check uses cached state (no extra DB query per tenant)
- [ ] Job logs which tenants were skipped and why

### US-5: Frontend Hides Disabled Modules
**As a** user of a tenant with Blog module disabled
**I want to** not see Blog-related navigation or pages
**So that** my workspace is clean and focused

**Acceptance Criteria:**
- [ ] Blog navigation items hidden from sidebar
- [ ] Direct URL navigation to /portal/blog/* shows "Module not available" page
- [ ] Other modules function normally
- [ ] When Blog is re-enabled, navigation reappears without page reload (SignalR)

---

## 6. Open Questions

| # | Question | Impact | Proposed Answer |
|---|----------|--------|----------------|
| 1 | Should feature management itself be audited in the Activity Timeline? | Low | Yes, via existing IAuditableCommand pattern |
| 2 | Should there be an API to check multiple features in one call (batch)? | Medium | Not for MVP; single checks with per-request caching suffice |
| 3 | Should disabled module endpoints return a `Retry-After` header (hinting the feature could be re-enabled)? | Low | No, keep it simple with 403 |
| 4 | How to handle in-flight requests when a module is disabled mid-session? | Low | Current request completes; next request sees updated state |
| 5 | Should there be a "preview" mode where platform admin can see what a tenant admin sees? | Medium | Nice to have, not for MVP |

---

## 7. Architecture Decisions

### AD-1: Code-Defined Catalog, DB-Stored Overrides
- **Decision**: Module/feature definitions in C# (`IModuleDefinition`), override values in DB
- **Rationale**: Type safety, compile-time validation, no schema changes for new features. ABP uses this pattern successfully.
- **Trade-off**: New features require code deployment (acceptable for NOIR)

### AD-2: ABP Provider Chain Concept + MS.FeatureManagement-Inspired Interface
- **Decision**: Custom `IFeatureChecker` with resolution chain (Tenant → Platform → Default)
- **Rationale**: ABP's chain is proven at scale. MS.FeatureManagement's `IVariantFeatureManager` is too generic for NOIR's simpler needs.
- **Trade-off**: Custom implementation vs. library dependency. Custom is simpler for NOIR's specific use case.

### AD-3: MediatR Pipeline Behavior for CQRS Gating
- **Decision**: `[RequiresFeature]` attribute + `FeatureCheckBehavior` pipeline
- **Rationale**: Consistent with NOIR's existing `ValidationBehavior`, `AuditBehavior` pipeline patterns. Declarative, testable.
- **Trade-off**: Requires attribute on every command/query that's feature-gated

### AD-4: Two Separate Tables (Availability + Toggle) vs. Single Table
- **Decision**: Two tables: `TenantModuleAvailability` (platform admin) and `TenantModuleToggle` (tenant admin)
- **Rationale**: Separation of concerns. Platform admin actions don't mix with tenant admin actions. Clear audit trail per authority level.
- **Trade-off**: More tables, but clearer data model. Resolution logic needs to join both.

### AD-5: FusionCache + Per-Request Dictionary (Two-Layer Cache)
- **Decision**: Use existing FusionCache for cross-request caching, request-scoped dictionary for per-request consistency
- **Rationale**: FusionCache already used in NOIR (TenantSettingsService). Per-request dictionary ensures consistent state within a single request.
- **Trade-off**: Slightly more memory per request (dictionary of ~50 features is negligible)

---

## 8. Proposed Module Definitions (All 33)

### Core Modules (Always-On, Cannot Be Disabled)
| Module Name | Display Name | Features |
|-------------|-------------|----------|
| `Core.Auth` | Authentication | Login, Signup, PasswordReset, OTP |
| `Core.Users` | User Management | CRUD, Profile, Avatar |
| `Core.Roles` | Role Management | CRUD, PermissionAssignment |
| `Core.Permissions` | Permissions | PermissionList, PermissionCheck |
| `Core.Dashboard` | Dashboard | Metrics, Overview |
| `Core.Settings` | Settings | TenantSettings, PlatformSettings |
| `Core.Audit` | Activity Timeline | AuditLog, ActivityTimeline |
| `Core.Notifications` | Notifications | RealTime, Preferences |

### Toggleable Modules
| Module Name | Display Name | Default | Key Features |
|-------------|-------------|---------|-------------|
| `Ecommerce.Products` | Products | Enabled | CRUD, Variants, Images, Options |
| `Ecommerce.Categories` | Categories | Enabled | CRUD, Hierarchy, Slugs |
| `Ecommerce.Brands` | Brands | Enabled | CRUD, Slugs |
| `Ecommerce.Attributes` | Product Attributes | Enabled | 13 AttributeTypes, FilterIndex |
| `Ecommerce.Cart` | Shopping Cart | Enabled | Guest, Auth, MergeCart |
| `Ecommerce.Checkout` | Checkout | Enabled | Address, Shipping, Payment |
| `Ecommerce.Orders` | Orders | Enabled | Lifecycle (7 commands), Status |
| `Ecommerce.Payments` | Payments | Enabled | VNPay, MoMo, ZaloPay, COD |
| `Ecommerce.Inventory` | Inventory | Enabled | Receipts (In/Out), Draft/Confirm |
| `Ecommerce.Promotions` | Promotions | Enabled | Discounts |
| `Ecommerce.Reviews` | Product Reviews | Enabled | CRUD, Ratings |
| `Ecommerce.Customers` | Customers | Enabled | CRUD, Profiles |
| `Ecommerce.CustomerGroups` | Customer Groups | Enabled | Segmentation |
| `Ecommerce.Wishlist` | Wishlists | Enabled | SavedProducts |
| `Content.Blog` | Blog | Enabled | Posts, Editor |
| `Content.BlogCategories` | Blog Categories | Enabled | CRUD |
| `Content.BlogTags` | Blog Tags | Enabled | CRUD |
| `Platform.Tenants` | Tenant Management | Enabled | CRUD (platform admin only) |
| `Platform.EmailTemplates` | Email Templates | Enabled | CRUD, Editor |
| `Platform.LegalPages` | Legal Pages | Enabled | Terms, Privacy |
| `Analytics.Reports` | Reports | Enabled | Analytics, Export |
| `System.DeveloperLogs` | Developer Logs | Enabled | RealTime, LevelControl |

---

## 9. Next Steps

After requirements approval:
1. **`/sc:design`** — Architecture design with detailed class diagrams, DB schema, API contracts
2. **`/sc:workflow`** — Implementation workflow with task breakdown
3. **`/sc:implement`** — Phased implementation (Backend → Frontend → Integration tests)

---

## 10. Reference Research

- [Module & Feature Management Systems - Comprehensive Research](module-feature-management-systems.md)
- ABP Framework: Provider chain, `[RequiresFeature]`, `FeatureDefinitionProvider`
- MS.FeatureManagement: `IVariantFeatureManager`, `[FeatureGate]`, `IFeatureDefinitionProvider`
- OrchardCore: Shell descriptor model (rejected - incompatible architecture)
- Flagsmith/Unleash: External service model (rejected - overkill)
