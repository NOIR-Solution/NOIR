# Feature Management System - Implementation Workflow

> Date: 2026-02-21
> Design: [feature-management-design.md](feature-management-design.md)
> Requirements: [feature-management-requirements.md](feature-management-requirements.md)

---

## Execution Summary

| Phase | Tasks | Team Pattern | Estimated Files | Quality Gate |
|-------|-------|-------------|-----------------|-------------|
| **Phase 1** | Domain + Entity Foundation | `backend-dev` + `test-writer` | 12 new | `dotnet build` |
| **Phase 2** | Module Catalog + FeatureChecker | `backend-dev` + `test-writer` | 38 new | `dotnet build` + `dotnet test` |
| **Phase 3** | CQRS Commands/Queries + API | `backend-dev` + `test-writer` | 18 new, 5 modified | `dotnet build` + `dotnet test` |
| **Phase 4** | Wolverine Middleware + Endpoint Gating | `backend-dev` + `test-writer` | 3 new, 38 modified | `dotnet build` + `dotnet test` |
| **Phase 5** | Frontend (hooks, services, UI) | `frontend-dev` + `test-writer` | 10 new, 6 modified | `pnpm build` |
| **Phase 6** | Background Jobs + Seeders + Polish | `backend-dev` + `frontend-dev` | 2 new, 5 modified | Full quality gate |

**Total: ~85 new files, ~54 modified files across 6 phases**

---

## Phase 1: Domain + Entity Foundation

> **Goal**: Create the domain model, interfaces, and database table.
> **Dependencies**: None (first phase)
> **Team**: `backend-dev` + `test-writer`

### Task 1.1: Domain Interfaces & Types

**Create** the following files:

| # | File | Description |
|---|------|-------------|
| 1 | `src/NOIR.Domain/Common/FeatureDefinition.cs` | `FeatureDefinition` record (Name, DisplayNameKey, DescriptionKey, DefaultEnabled) |
| 2 | `src/NOIR.Domain/Common/RequiresFeatureAttribute.cs` | `[RequiresFeature("...")]` attribute for commands/queries |
| 3 | `src/NOIR.Domain/Interfaces/IModuleDefinition.cs` | Module definition interface (Name, Icon, IsCore, Features list) |
| 4 | `src/NOIR.Domain/Interfaces/IModuleCatalog.cs` | Catalog interface (GetAllModules, GetModule, GetFeature, IsCore, GetParentModuleName) |
| 5 | `src/NOIR.Domain/Interfaces/IFeatureChecker.cs` | Feature checker interface + `EffectiveFeatureState` record |

**Modify:**

| # | File | Change |
|---|------|--------|
| 6 | `src/NOIR.Domain/Common/ErrorCodes.cs` | Add `Feature` inner class with `NotAvailable`, `CoreCannotBeDisabled`, `ParentModuleDisabled`, `NotFound` |
| 7 | `src/NOIR.Domain/GlobalUsings.cs` | Add `global using NOIR.Domain.Interfaces;` if missing |

**Validation**: `dotnet build src/NOIR.Domain`

### Task 1.2: Entity + EF Configuration

| # | File | Description |
|---|------|-------------|
| 8 | `src/NOIR.Domain/Entities/TenantModuleState.cs` | Entity extending `TenantEntity<Guid>` with `FeatureName`, `IsAvailable`, `IsEnabled`, factory methods |
| 9 | `src/NOIR.Infrastructure/Persistence/Configurations/TenantModuleStateConfiguration.cs` | EF config with unique index `(TenantId, FeatureName)`, soft delete filter, audit fields |

**Modify:**

| # | File | Change |
|---|------|--------|
| 10 | `src/NOIR.Infrastructure/Persistence/ApplicationDbContext.cs` | Add `public DbSet<TenantModuleState> TenantModuleStates { get; set; }` |

**Validation**: `dotnet build src/NOIR.Infrastructure`

### Task 1.3: Database Migration

```bash
dotnet ef migrations add AddTenantModuleStates \
  --project src/NOIR.Infrastructure \
  --startup-project src/NOIR.Web \
  --context ApplicationDbContext \
  --output-dir Migrations/App
```

**Validation**: Inspect generated migration file, then `dotnet build src/NOIR.sln`

### Task 1.4: Domain Unit Tests

| # | File | Tests |
|---|------|-------|
| 11 | `tests/NOIR.Domain.UnitTests/Entities/TenantModuleStateTests.cs` | Create, SetAvailability, SetEnabled, factory methods |
| 12 | `tests/NOIR.Domain.UnitTests/Common/RequiresFeatureAttributeTests.cs` | Single feature, multiple features, attribute target |

**Validation**: `dotnet test tests/NOIR.Domain.UnitTests`

### Phase 1 Quality Gate

```bash
dotnet build src/NOIR.sln          # 0 errors
dotnet test src/NOIR.sln           # ALL pass
```

---

## Phase 2: Module Catalog + FeatureChecker

> **Goal**: Define all 33 modules and implement the feature resolution engine.
> **Dependencies**: Phase 1 complete
> **Team**: `backend-dev` (module defs) + `backend-dev` (services) + `test-writer`

### Task 2.1: Module Name Constants

| # | File | Description |
|---|------|-------------|
| 1 | `src/NOIR.Application/Modules/ModuleNames.cs` | Static class with nested classes: `Core`, `Ecommerce`, `Content`, `Platform`, `Analytics`. All 33 module name constants. |

### Task 2.2: Core Module Definitions (8 files) -- PARALLEL

All implement `IModuleDefinition, ISingletonService`. All have `IsCore = true`.

| # | File | Module Name |
|---|------|-------------|
| 2 | `src/NOIR.Application/Modules/Core/AuthModuleDefinition.cs` | `Core.Auth` |
| 3 | `src/NOIR.Application/Modules/Core/UsersModuleDefinition.cs` | `Core.Users` |
| 4 | `src/NOIR.Application/Modules/Core/RolesModuleDefinition.cs` | `Core.Roles` |
| 5 | `src/NOIR.Application/Modules/Core/PermissionsModuleDefinition.cs` | `Core.Permissions` |
| 6 | `src/NOIR.Application/Modules/Core/DashboardModuleDefinition.cs` | `Core.Dashboard` |
| 7 | `src/NOIR.Application/Modules/Core/SettingsModuleDefinition.cs` | `Core.Settings` |
| 8 | `src/NOIR.Application/Modules/Core/AuditModuleDefinition.cs` | `Core.Audit` |
| 9 | `src/NOIR.Application/Modules/Core/NotificationsModuleDefinition.cs` | `Core.Notifications` |

### Task 2.3: E-commerce Module Definitions (14 files) -- PARALLEL

All have `IsCore = false`, `DefaultEnabled = true`.

| # | File | Module Name | Features (child) |
|---|------|-------------|------------------|
| 10 | `ProductsModuleDefinition.cs` | `Ecommerce.Products` | Variants, Options, Import, Export |
| 11 | `CategoriesModuleDefinition.cs` | `Ecommerce.Categories` | Hierarchy, SEO |
| 12 | `BrandsModuleDefinition.cs` | `Ecommerce.Brands` | (none) |
| 13 | `AttributesModuleDefinition.cs` | `Ecommerce.Attributes` | FilterIndex |
| 14 | `CartModuleDefinition.cs` | `Ecommerce.Cart` | GuestCart, MergeCart |
| 15 | `CheckoutModuleDefinition.cs` | `Ecommerce.Checkout` | (none) |
| 16 | `OrdersModuleDefinition.cs` | `Ecommerce.Orders` | Returns, Cancellations |
| 17 | `PaymentsModuleDefinition.cs` | `Ecommerce.Payments` | VNPay, MoMo, ZaloPay, COD |
| 18 | `InventoryModuleDefinition.cs` | `Ecommerce.Inventory` | StockIn, StockOut |
| 19 | `PromotionsModuleDefinition.cs` | `Ecommerce.Promotions` | (none) |
| 20 | `ReviewsModuleDefinition.cs` | `Ecommerce.Reviews` | (none) |
| 21 | `CustomersModuleDefinition.cs` | `Ecommerce.Customers` | (none) |
| 22 | `CustomerGroupsModuleDefinition.cs` | `Ecommerce.CustomerGroups` | (none) |
| 23 | `WishlistModuleDefinition.cs` | `Ecommerce.Wishlist` | (none) |

### Task 2.4: Content + Platform + Analytics Module Definitions (8 files) -- PARALLEL

| # | File | Module Name |
|---|------|-------------|
| 24 | `src/NOIR.Application/Modules/Content/BlogModuleDefinition.cs` | `Content.Blog` |
| 25 | `src/NOIR.Application/Modules/Content/BlogCategoriesModuleDefinition.cs` | `Content.BlogCategories` |
| 26 | `src/NOIR.Application/Modules/Content/BlogTagsModuleDefinition.cs` | `Content.BlogTags` |
| 27 | `src/NOIR.Application/Modules/Platform/TenantsModuleDefinition.cs` | `Platform.Tenants` |
| 28 | `src/NOIR.Application/Modules/Platform/EmailTemplatesModuleDefinition.cs` | `Platform.EmailTemplates` |
| 29 | `src/NOIR.Application/Modules/Platform/LegalPagesModuleDefinition.cs` | `Platform.LegalPages` |
| 30 | `src/NOIR.Application/Modules/Analytics/ReportsModuleDefinition.cs` | `Analytics.Reports` |
| 31 | `src/NOIR.Application/Modules/Analytics/DeveloperLogsModuleDefinition.cs` | `Analytics.DeveloperLogs` |

### Task 2.5: ModuleCatalog Implementation

| # | File | Description |
|---|------|-------------|
| 32 | `src/NOIR.Infrastructure/Services/ModuleCatalog.cs` | `ISingletonService`. Aggregates all `IModuleDefinition` via constructor injection. Provides O(1) lookup by name. Computes parent module names by walking dots. |

### Task 2.6: FeatureChecker Implementation

| # | File | Description |
|---|------|-------------|
| 33 | `src/NOIR.Infrastructure/Services/FeatureChecker.cs` | `IScopedService`. Per-request dictionary cache + FusionCache. Resolution chain: Core bypass → DB overrides → code defaults. Parent hierarchy enforcement. |

**Modify:**

| # | File | Change |
|---|------|--------|
| 34 | `src/NOIR.Infrastructure/Caching/CacheKeys.cs` | Add `public static string TenantFeatures(string? tenantId) => $"features:tenant:{tenantId ?? "platform"}";` |

### Task 2.7: FeatureCacheInvalidator

| # | File | Description |
|---|------|-------------|
| 35 | `src/NOIR.Infrastructure/Services/FeatureCacheInvalidator.cs` | `IScopedService`. Removes FusionCache entry + sends SignalR notification to tenant group. |

### Task 2.8: Specifications

| # | File | Description |
|---|------|-------------|
| 36 | `src/NOIR.Application/Features/FeatureManagement/Specifications/TenantModuleStateSpecs.cs` | `TenantModuleStateByTenantSpec` and `TenantModuleStateByFeatureSpec` |

### Task 2.9: Unit Tests -- PARALLEL with Tasks 2.5-2.8

| # | File | Tests |
|---|------|-------|
| 37 | `tests/NOIR.Application.UnitTests/Modules/ModuleCatalogTests.cs` | All 33 modules registered, no duplicate names, core modules identified, parent name resolution |
| 38 | `tests/NOIR.Application.UnitTests/Modules/FeatureCheckerTests.cs` | Core always enabled, default state, platform unavailable overrides tenant, parent off disables children, cache behavior |

### Phase 2 Quality Gate

```bash
dotnet build src/NOIR.sln          # 0 errors
dotnet test src/NOIR.sln           # ALL pass (including new tests)
```

---

## Phase 3: CQRS Commands/Queries + API Endpoints

> **Goal**: Create the feature management API with full CQRS pipeline.
> **Dependencies**: Phase 2 complete
> **Team**: `backend-dev` + `test-writer`

### Task 3.1: DTOs

| # | File |
|---|------|
| 1 | `src/NOIR.Application/Features/FeatureManagement/DTOs/ModuleCatalogDto.cs` |
| 2 | `src/NOIR.Application/Features/FeatureManagement/DTOs/ModuleDto.cs` |
| 3 | `src/NOIR.Application/Features/FeatureManagement/DTOs/FeatureDto.cs` |
| 4 | `src/NOIR.Application/Features/FeatureManagement/DTOs/TenantFeatureStateDto.cs` |

### Task 3.2: Commands -- PARALLEL

**SetModuleAvailability** (platform admin):

| # | File |
|---|------|
| 5 | `src/NOIR.Application/Features/FeatureManagement/Commands/SetModuleAvailability/SetModuleAvailabilityCommand.cs` |
| 6 | `src/NOIR.Application/Features/FeatureManagement/Commands/SetModuleAvailability/SetModuleAvailabilityCommandHandler.cs` |
| 7 | `src/NOIR.Application/Features/FeatureManagement/Commands/SetModuleAvailability/SetModuleAvailabilityCommandValidator.cs` |

**ToggleModule** (tenant admin):

| # | File |
|---|------|
| 8 | `src/NOIR.Application/Features/FeatureManagement/Commands/ToggleModule/ToggleModuleCommand.cs` |
| 9 | `src/NOIR.Application/Features/FeatureManagement/Commands/ToggleModule/ToggleModuleCommandHandler.cs` |
| 10 | `src/NOIR.Application/Features/FeatureManagement/Commands/ToggleModule/ToggleModuleCommandValidator.cs` |

### Task 3.3: Queries -- PARALLEL

| # | File | Description |
|---|------|-------------|
| 11 | `src/NOIR.Application/Features/FeatureManagement/Queries/GetModuleCatalog/GetModuleCatalogQuery.cs` | Returns full catalog (definitions only, no tenant state) |
| 12 | `src/NOIR.Application/Features/FeatureManagement/Queries/GetModuleCatalog/GetModuleCatalogQueryHandler.cs` | |
| 13 | `src/NOIR.Application/Features/FeatureManagement/Queries/GetTenantFeatureStates/GetTenantFeatureStatesQuery.cs` | Platform admin: get states for specific tenant |
| 14 | `src/NOIR.Application/Features/FeatureManagement/Queries/GetTenantFeatureStates/GetTenantFeatureStatesQueryHandler.cs` | |
| 15 | `src/NOIR.Application/Features/FeatureManagement/Queries/GetCurrentTenantFeatures/GetCurrentTenantFeaturesQuery.cs` | Frontend: current tenant features |
| 16 | `src/NOIR.Application/Features/FeatureManagement/Queries/GetCurrentTenantFeatures/GetCurrentTenantFeaturesQueryHandler.cs` | |

### Task 3.4: Exception + Permissions

| # | File | Change |
|---|------|--------|
| 17 | `src/NOIR.Application/Common/Exceptions/FeatureNotAvailableException.cs` | **CREATE** - New exception type |
| 18 | `src/NOIR.Domain/Common/Permissions.cs` | **MODIFY** - Add `FeaturesRead = "features:read"` and `FeaturesUpdate = "features:update"` |

### Task 3.5: API Endpoints

| # | File | Description |
|---|------|-------------|
| 19 | `src/NOIR.Web/Endpoints/FeatureManagementEndpoints.cs` | **CREATE** - 5 endpoints: GET current-tenant, GET catalog, GET tenant/{id}, PUT availability, PUT toggle |

**Modify:**

| # | File | Change |
|---|------|--------|
| 20 | `src/NOIR.Web/Program.cs` | Add `app.MapFeatureManagementEndpoints();` to endpoint registration section |

### Task 3.6: Command/Query Unit Tests

| # | File |
|---|------|
| 21 | `tests/NOIR.Application.UnitTests/Features/FeatureManagement/Commands/SetModuleAvailabilityCommandTests.cs` |
| 22 | `tests/NOIR.Application.UnitTests/Features/FeatureManagement/Commands/ToggleModuleCommandTests.cs` |
| 23 | `tests/NOIR.Application.UnitTests/Features/FeatureManagement/Queries/GetCurrentTenantFeaturesQueryTests.cs` |

### Phase 3 Quality Gate

```bash
dotnet build src/NOIR.sln          # 0 errors
dotnet test src/NOIR.sln           # ALL pass
```

---

## Phase 4: Wolverine Middleware + Endpoint Gating

> **Goal**: Gate all non-core commands and endpoints behind feature checks.
> **Dependencies**: Phase 3 complete
> **Team**: `backend-dev` + `test-writer`

### Task 4.1: Wolverine Middleware

| # | File | Description |
|---|------|-------------|
| 1 | `src/NOIR.Infrastructure/Middleware/FeatureCheckMiddleware.cs` | **CREATE** - Wolverine middleware. `BeforeAsync()` reads `[RequiresFeature]` attribute, calls `IFeatureChecker.IsEnabledAsync()`, throws `FeatureNotAvailableException` if disabled. |

**Modify:**

| # | File | Change |
|---|------|--------|
| 2 | `src/NOIR.Web/Program.cs` | Add `opts.Policies.AddMiddleware<FeatureCheckMiddleware>();` in `UseWolverine` config |
| 3 | `src/NOIR.Web/Middleware/ExceptionHandlingMiddleware.cs` | Add `FeatureNotAvailableException` case → 403 Forbidden with `ErrorCodes.Feature.NotAvailable` |

### Task 4.2: Endpoint Filter

| # | File | Description |
|---|------|-------------|
| 4 | `src/NOIR.Web/Filters/RequireFeatureFilter.cs` | **CREATE** - `IEndpointFilter` + `RequireFeature()` extension method for `RouteHandlerBuilder` and `RouteGroupBuilder` |

### Task 4.3: Apply `.RequireFeature()` to All Endpoint Groups

**Modify 24 endpoint files** to add `.RequireFeature()` on their `MapGroup()` call:

| # | Endpoint File | Feature Gate |
|---|---------------|-------------|
| 5 | `BlogEndpoints.cs` | `.RequireFeature(ModuleNames.Content.Blog)` |
| 6 | `ProductEndpoints.cs` | `.RequireFeature(ModuleNames.Ecommerce.Products)` |
| 7 | `ProductCategoryEndpoints.cs` | `.RequireFeature(ModuleNames.Ecommerce.Categories)` |
| 8 | `BrandEndpoints.cs` | `.RequireFeature(ModuleNames.Ecommerce.Brands)` |
| 9 | `ProductAttributeEndpoints.cs` | `.RequireFeature(ModuleNames.Ecommerce.Attributes)` |
| 10 | `ProductFilterEndpoints.cs` | `.RequireFeature(ModuleNames.Ecommerce.Attributes)` |
| 11 | `FilterAnalyticsEndpoints.cs` | `.RequireFeature(ModuleNames.Ecommerce.Attributes)` |
| 12 | `CartEndpoints.cs` | `.RequireFeature(ModuleNames.Ecommerce.Cart)` |
| 13 | `CheckoutEndpoints.cs` | `.RequireFeature(ModuleNames.Ecommerce.Checkout)` |
| 14 | `OrderEndpoints.cs` | `.RequireFeature(ModuleNames.Ecommerce.Orders)` |
| 15 | `PaymentEndpoints.cs` | `.RequireFeature(ModuleNames.Ecommerce.Payments)` |
| 16 | `InventoryEndpoints.cs` | `.RequireFeature(ModuleNames.Ecommerce.Inventory)` |
| 17 | `PromotionEndpoints.cs` | `.RequireFeature(ModuleNames.Ecommerce.Promotions)` |
| 18 | `ReviewEndpoints.cs` | `.RequireFeature(ModuleNames.Ecommerce.Reviews)` |
| 19 | `CustomerEndpoints.cs` | `.RequireFeature(ModuleNames.Ecommerce.Customers)` |
| 20 | `CustomerGroupEndpoints.cs` | `.RequireFeature(ModuleNames.Ecommerce.CustomerGroups)` |
| 21 | `WishlistEndpoints.cs` | `.RequireFeature(ModuleNames.Ecommerce.Wishlist)` |
| 22 | `ShippingEndpoints.cs` | `.RequireFeature(ModuleNames.Ecommerce.Checkout)` |
| 23 | `ShippingProviderEndpoints.cs` | `.RequireFeature(ModuleNames.Ecommerce.Checkout)` |
| 24 | `TenantEndpoints.cs` | `.RequireFeature(ModuleNames.Platform.Tenants)` |
| 25 | `EmailTemplateEndpoints.cs` | `.RequireFeature(ModuleNames.Platform.EmailTemplates)` |
| 26 | `LegalPageEndpoints.cs` | `.RequireFeature(ModuleNames.Platform.LegalPages)` |
| 27 | `ReportEndpoints.cs` | `.RequireFeature(ModuleNames.Analytics.Reports)` |
| 28 | `DeveloperLogEndpoints.cs` | `.RequireFeature(ModuleNames.Analytics.DeveloperLogs)` |

**DO NOT add `.RequireFeature()` to core endpoints:**
- `AuthEndpoints.cs` (Core.Auth)
- `UserEndpoints.cs` (Core.Users)
- `RoleEndpoints.cs` (Core.Roles)
- `PermissionEndpoints.cs` (Core.Permissions)
- `AuditEndpoints.cs` (Core.Audit)
- `NotificationEndpoints.cs` (Core.Notifications)
- `DashboardEndpoints.cs` (Core.Dashboard)
- `PlatformSettingsEndpoints.cs` (Core.Settings)
- `TenantSettingsEndpoints.cs` (Core.Settings)
- `FeatureManagementEndpoints.cs` (self - must always be accessible)
- `FileEndpoints.cs` (shared utility)
- `MediaEndpoints.cs` (shared utility)
- `FeedEndpoints.cs` (public - RSS/Sitemap, gate in handler if needed)
- `PublicLegalPageEndpoints.cs` (public)
- `DevEndpoints.cs` (development only)

### Task 4.4: Add `[RequiresFeature]` to Key Commands (optional, for defense-in-depth)

Add `[RequiresFeature(ModuleNames.Content.Blog)]` to Blog commands as a representative example:
- `CreatePostCommand`
- `UpdatePostCommand`
- `DeletePostCommand`
- `CreateCategoryCommand` (Blog)
- `CreateTagCommand` (Blog)

> **Note**: The endpoint filter is the primary gate. `[RequiresFeature]` on commands is defense-in-depth for commands that might be invoked via other channels (SignalR, background jobs, etc.). Can be expanded to all commands in a follow-up PR.

### Task 4.5: Middleware + Filter Tests

| # | File | Tests |
|---|------|-------|
| 29 | `tests/NOIR.Application.UnitTests/Behaviors/FeatureCheckMiddlewareTests.cs` | Attribute present + enabled → passes, attribute present + disabled → throws, no attribute → passes, core feature → always passes |
| 30 | `tests/NOIR.Application.UnitTests/Filters/RequireFeatureFilterTests.cs` | Enabled → passes, disabled → 403, multiple features |

### Phase 4 Quality Gate

```bash
dotnet build src/NOIR.sln          # 0 errors
dotnet test src/NOIR.sln           # ALL pass
```

---

## Phase 5: Frontend

> **Goal**: Feature hooks, navigation gating, route guards, admin UI, localization.
> **Dependencies**: Phase 3 complete (API endpoints available)
> **Team**: `frontend-dev` + `test-writer`

### Task 5.1: TypeScript Types + API Service -- PARALLEL

| # | File | Description |
|---|------|-------------|
| 1 | `src/NOIR.Web/frontend/src/types/features.ts` | **CREATE** - `EffectiveFeatureState`, `ModuleDto`, `FeatureDto`, `ModuleCatalogDto`, `TenantFeatureStateDto` |
| 2 | `src/NOIR.Web/frontend/src/services/features.ts` | **CREATE** - `getCurrentTenantFeatures()`, `getModuleCatalog()`, `getTenantFeatureStates()`, `setModuleAvailability()`, `toggleModule()` |

**Modify:**

| # | File | Change |
|---|------|--------|
| 3 | `src/NOIR.Web/frontend/src/types/index.ts` | Add `export * from './features'` |

### Task 5.2: React Hooks

| # | File | Description |
|---|------|-------------|
| 4 | `src/NOIR.Web/frontend/src/hooks/useFeatures.ts` | **CREATE** - `useFeatures()` (TanStack Query + cache), `useFeature(name)` (simple boolean), `featureKeys` for query key management |

### Task 5.3: Route Guard Component

| # | File | Description |
|---|------|-------------|
| 5 | `src/NOIR.Web/frontend/src/components/guards/FeatureGuard.tsx` | **CREATE** - Wraps routes, shows "Module not available" page if feature disabled. Uses `useFeature()`. |

### Task 5.4: Sidebar Integration

**Modify:**

| # | File | Change |
|---|------|--------|
| 6 | `src/NOIR.Web/frontend/src/components/portal/Sidebar.tsx` | Add `feature?: string` to `NavItem` interface. Add `feature` key to each nav item that maps to a toggleable module. Add `useFeatures()` hook and filter items by `isFeatureEnabled(item.feature)` alongside existing permission check. |

### Task 5.5: Modules Settings Tab

| # | File | Description |
|---|------|-------------|
| 7 | `src/NOIR.Web/frontend/src/portal-app/settings/components/tenant-settings/ModulesSettingsTab.tsx` | **CREATE** - Tree view with Switch toggles. Platform admin sees "Available" + "Enabled". Tenant admin sees "Enabled" only. Core modules grayed out. Optimistic mutations via TanStack Query. |

**Modify:**

| # | File | Change |
|---|------|--------|
| 8 | `src/NOIR.Web/frontend/src/portal-app/settings/features/tenant-settings/TenantSettingsPage.tsx` | Add "Modules" tab (using `Blocks` icon). Import `ModulesSettingsTab`. Add `TabsTrigger` and `TabsContent`. |

### Task 5.6: Route Guard Integration

**Modify** the router to wrap feature-gated page groups:

| # | File | Change |
|---|------|--------|
| 9 | Router file (likely `src/NOIR.Web/frontend/src/App.tsx` or `routes.tsx`) | Wrap Blog routes with `<FeatureGuard feature="Content.Blog">`, E-commerce routes with respective guards. Core routes unchanged. |

### Task 5.7: SignalR Integration

**Modify** the existing SignalR notification handler:

| # | File | Change |
|---|------|--------|
| 10 | SignalR connection handler (NotificationContext or hook) | On `ReceiveNotification` with type `features_updated`, call `queryClient.invalidateQueries({ queryKey: ['features', 'current-tenant'] })` |

### Task 5.8: Localization (EN + VI)

**Modify:**

| # | File | Change |
|---|------|--------|
| 11 | `src/NOIR.Web/frontend/public/locales/en/common.json` | Add `features.*` and `modules.*` keys + `tenantSettings.tabs.modules` |
| 12 | `src/NOIR.Web/frontend/public/locales/vi/common.json` | Add corresponding Vietnamese translations |

### Phase 5 Quality Gate

```bash
cd src/NOIR.Web/frontend && pnpm run build       # 0 errors, 0 warnings
cd src/NOIR.Web/frontend && pnpm build-storybook  # 0 errors
```

**Manual checks:**
- [ ] All interactive elements have `cursor-pointer`
- [ ] All icon-only buttons have `aria-label`
- [ ] No hardcoded strings (all use `t('key')`)
- [ ] Both EN and VI have matching keys

---

## Phase 6: Background Jobs + Seeders + Polish

> **Goal**: Feature-aware jobs, seeder integration, architecture tests, final validation.
> **Dependencies**: Phases 4 + 5 complete
> **Team**: `backend-dev` + `frontend-dev`

### Task 6.1: TenantJobRunner Helper

| # | File | Description |
|---|------|-------------|
| 1 | `src/NOIR.Infrastructure/Services/TenantJobRunner.cs` | **CREATE** - `ITenantJobRunner` + `TenantJobRunner : IScopedService`. Iterates active tenants, sets tenant context, checks feature, executes action. |

### Task 6.2: Update Existing Background Jobs

**Modify:**

| # | File | Change |
|---|------|--------|
| 2 | `src/NOIR.Infrastructure/Products/ProductFilterIndexMaintenanceJob.cs` | Use `ITenantJobRunner.RunForEnabledTenantsAsync(ModuleNames.Ecommerce.Attributes, ...)` |
| 3 | `src/NOIR.Infrastructure/Products/ProductFilterIndexReindexJob.cs` | Same pattern |

> **Note**: `AuditRetentionJob.cs` runs at platform level (not tenant-scoped), so no feature gate needed.

### Task 6.3: Permission Seeder Update

**Modify:**

| # | File | Change |
|---|------|--------|
| 4 | Permission seeder (or PermissionDefinitions) | Add `features:read` and `features:update` to the permission definitions. Assign to Tenant Admin and Platform Admin roles. |

### Task 6.4: Architecture Tests

| # | File | Tests |
|---|------|-------|
| 5 | `tests/NOIR.Architecture.Tests/FeatureManagementArchitectureTests.cs` | **CREATE** - Verify: no duplicate module names, all non-core endpoints have `.RequireFeature()`, all module definitions registered as ISingletonService |

### Task 6.5: Integration Tests

| # | File | Tests |
|---|------|-------|
| 6 | `tests/NOIR.IntegrationTests/Features/FeatureManagement/FeatureManagementTests.cs` | **CREATE** - End-to-end: create tenant state → check feature → verify disabled endpoint returns 403 → re-enable → verify 200 |

### Phase 6 Quality Gate (FULL)

```bash
dotnet build src/NOIR.sln                              # 0 errors
dotnet test src/NOIR.sln                               # ALL pass, zero skipped
cd src/NOIR.Web/frontend && pnpm run build             # 0 errors, 0 warnings
cd src/NOIR.Web/frontend && pnpm build-storybook       # 0 errors
```

---

## Dependency Graph

```
Phase 1: Domain + Entity
    │
    ▼
Phase 2: Module Catalog + FeatureChecker
    │
    ├──────────────────┐
    ▼                  ▼
Phase 3: CQRS + API   Phase 5.1-5.2: Types + Hooks (can start after Phase 2)
    │                  │
    ▼                  ▼
Phase 4: Middleware    Phase 5.3-5.8: UI + Integration
    │                  │
    └──────────────────┘
              │
              ▼
Phase 6: Jobs + Seeders + Polish
              │
              ▼
         COMPLETE
```

> **Parallelization opportunity**: Phase 5 (Tasks 5.1-5.2) can start as soon as Phase 3 API endpoints are defined. The remaining frontend tasks (5.3-5.8) need the API to be functional.

---

## Checkpoint Checklist

After each phase, verify:

- [ ] `dotnet build src/NOIR.sln` → 0 errors
- [ ] `dotnet test src/NOIR.sln` → ALL pass
- [ ] No `using` statements in .cs files (all in GlobalUsings.cs)
- [ ] All new entities use correct base class
- [ ] All unique constraints include TenantId (Rule 18)
- [ ] All Specifications use `.TagWith("MethodName")` (Rule 2)
- [ ] All mutations call `_unitOfWork.SaveChangesAsync()` (Rule 7)
- [ ] All commands with mutations implement `IAuditableCommand` (Rule 11)

After Phase 5:
- [ ] `pnpm run build` → 0 errors, 0 warnings
- [ ] `pnpm build-storybook` → 0 errors
- [ ] No hardcoded user-facing strings
- [ ] Both EN + VI localization files have matching keys
- [ ] All interactive elements have `cursor-pointer`
- [ ] All icon-only buttons have `aria-label`

---

## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| Wolverine middleware ordering conflicts with existing middleware | Register `FeatureCheckMiddleware` BEFORE `HandlerAuditMiddleware` but AFTER `LoggingMiddleware` |
| FusionCache serialization issues with `TenantModuleStateRow` dictionary | Use simple record types that serialize cleanly to JSON |
| Endpoint filter breaks existing tests | Ensure test environment seeds all features as enabled (or mock `IFeatureChecker` to return true) |
| Module name typos cause silent failures | Architecture test verifies all `ModuleNames` constants match `IModuleDefinition.Name` values |
| Frontend feature check on first load before API call completes | Default `isFeatureEnabled()` returns `true` for unknown features (graceful degradation) |
| SignalR notification triggers infinite re-render | TanStack Query's `staleTime: 5min` prevents rapid refetching; only invalidates, doesn't force immediate refetch |

---

## Definition of Done

The Feature Management system is complete when:

1. All 33 modules are code-defined with `IModuleDefinition`
2. Platform admin can set module availability per tenant via API
3. Tenant admin can toggle modules on/off via Tenant Settings UI
4. Disabled modules: navigation hidden, API returns 403, data preserved
5. Core modules cannot be disabled (UI and API enforce this)
6. Parent module off = all children off
7. Background jobs respect feature state per tenant
8. All existing tests pass (10,800+)
9. New unit tests cover resolution logic, middleware, commands, queries
10. Architecture tests verify no gaps in feature gating
11. Frontend build and Storybook build pass with 0 errors
12. Localization complete (EN + VI)

---

> **Next Step**: Use `/sc:implement` to begin Phase 1.
