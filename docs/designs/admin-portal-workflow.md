# Admin Portal Enhancement — Implementation Workflow

> Generated: 2026-02-28. Execution plan for [admin-portal-enhancement-v28-feb-design.md](admin-portal-enhancement-v28-feb-design.md).
> Total: 6 phases, ~186 files, ~41 test files.

---

## Pre-Flight Checklist

Before starting any phase, verify:

```bash
dotnet build src/NOIR.sln             # 0 errors
dotnet test src/NOIR.sln              # 11,341+ pass
cd src/NOIR.Web/frontend && pnpm run build  # 0 errors
```

### Codebase State (Verified 2026-02-28)

| Item | Status |
|------|--------|
| `ModuleNames.Dashboard` nested class | Does NOT exist (only `Core.Dashboard` constant) |
| `Permissions.DashboardRead` | Does NOT exist |
| `Features/Media/` directory | Does NOT exist |
| `Features/Search/` directory | Does NOT exist |
| `useBulkSelection.ts` hook | Does NOT exist |
| `components/bulk-actions/` | Does NOT exist |
| `components/import-export/` | Does NOT exist |
| `ImportHistory` entity | Does NOT exist |
| `portal-app/media/` directory | Does NOT exist |
| ClosedXML NuGet package | NOT installed |
| xlsx (SheetJS) npm package | NOT installed |
| `MaxBulkOperationSize` constant | `ProductConstants.cs` = 1000 |

---

## Phase 0: Media Storage Bug Fixes

**Priority:** Critical (pre-req for Phase 2)
**Risk:** Low
**Team:** Solo (backend-dev)

### Task 0.1 — Fix `GetPublicUrl()` for cloud providers

```
File: src/NOIR.Infrastructure/Services/FileStorageService.cs
Line: ~121 (GetPublicUrl method)
```

- [ ] Add provider check: if S3/Azure → return direct bucket/CDN URL
- [ ] Local storage → keep existing relative path behavior
- [ ] No behavior change for local dev (current default)

### Task 0.2 — Fix CDN URL double-prefix

```
File: src/NOIR.Web/Endpoints/MediaEndpoints.cs
Line: ~123 (absoluteUrl construction)
```

- [ ] Add `StartsWith("http")` check before prepending base URL
- [ ] Already-absolute URLs (from cloud providers) pass through unchanged

### Task 0.3 — Fix CDN filename vs storagePath

```
File: src/NOIR.Infrastructure/Media/ImageProcessorService.cs
Line: ~349 (CDN URL construction in SaveVariantAsync)
```

- [ ] Replace `fileName` → `storagePath` in CDN URL construction
- [ ] Preserves folder structure: `products/product-123/image-thumb.webp`

### Task 0.4 — Tests for bug fixes

```
New: tests/NOIR.Application.UnitTests/Services/FileStorageServiceTests.cs (if not exists)
```

- [ ] Test: Local provider returns relative URL `/media/path`
- [ ] Test: S3 provider returns `https://{bucket}.s3.{region}.amazonaws.com/path`
- [ ] Test: Azure provider returns `https://{account}.blob.core.windows.net/{container}/path`
- [ ] Test: CDN URL uses full storagePath, not just filename

### Quality Gate 0

```bash
dotnet build src/NOIR.sln   # 0 errors
dotnet test src/NOIR.sln     # ALL pass
```

**Checkpoint:** Commit `fix(media): resolve 3 cloud storage URL bugs`

---

## Phase 1: Dashboard (Modular Widget Architecture)

**Priority:** High
**Risk:** Low (backend exists, frontend is UI)
**Team:** `backend-dev` + `frontend-dev` (parallel after Task 1.2)

### Stage 1A: Backend (backend-dev)

#### Task 1.1 — Add Dashboard sub-feature constants

```
Modify: src/NOIR.Application/Modules/ModuleNames.cs
```

- [ ] Add `public static class Dashboard` with `Core`, `Ecommerce`, `Blog`, `Inventory` constants
- [ ] These are convention-only constants (Option A from design) — no new module definitions

#### Task 1.2 — Add `Permissions.DashboardRead`

```
Modify: src/NOIR.Application/Common/Permissions/Permissions.cs (find actual location)
```

- [ ] Add `DashboardRead` permission constant
- [ ] Add to `Permissions.Groups.Dashboard` group
- [ ] Add to `Permissions.All` collection
- [ ] Add to default admin role permissions
- [ ] **CRITICAL:** Update `PermissionsTests.All_ShouldContainAllPermissions` expectedCount

#### Task 1.3 — Create `GetCoreDashboardQuery` + Handler + DTOs

```
New: src/NOIR.Application/Features/Dashboard/DTOs/CoreDashboardDtos.cs
New: src/NOIR.Application/Features/Dashboard/Queries/GetCoreDashboard/GetCoreDashboardQuery.cs
New: src/NOIR.Application/Features/Dashboard/Queries/GetCoreDashboard/GetCoreDashboardQueryHandler.cs
```

- [ ] `CoreDashboardDto` with `QuickActionCountsDto`, `ActivityFeedItemDto[]`, `SystemHealthDto?`
- [ ] Handler: 4 count queries via `Task.WhenAll()` for quick actions
- [ ] Handler: Query `AuditLog` for recent activity feed
- [ ] Handler: `SystemHealth` only when `ICurrentUser.IsPlatformAdmin`
- [ ] All queries use `.TagWith("Dashboard_Core_*")`

**Depends on:** Task 1.2 (permissions)

#### Task 1.4 — Create `GetEcommerceDashboardQuery` + Handler

```
New: src/NOIR.Application/Features/Dashboard/Queries/GetEcommerceDashboard/GetEcommerceDashboardQuery.cs
New: src/NOIR.Application/Features/Dashboard/Queries/GetEcommerceDashboard/GetEcommerceDashboardQueryHandler.cs
```

- [ ] Delegates to existing `IDashboardQueryService.GetMetricsAsync()`
- [ ] Returns existing `DashboardMetricsDto`
- [ ] Wrapper only — no new business logic

#### Task 1.5 — Create `GetBlogDashboardQuery` + Handler + DTOs

```
New: src/NOIR.Application/Features/Dashboard/DTOs/BlogDashboardDtos.cs
New: src/NOIR.Application/Features/Dashboard/Queries/GetBlogDashboard/GetBlogDashboardQuery.cs
New: src/NOIR.Application/Features/Dashboard/Queries/GetBlogDashboard/GetBlogDashboardQueryHandler.cs
```

- [ ] `BlogDashboardDto`: post counts by status, top posts, publishing trend
- [ ] Handler: Count queries on `Post` entity grouped by status
- [ ] Handler: Top posts by view count (take 5)
- [ ] Handler: Publishing trend — group by `CreatedAt.Date` over `TrendDays`

#### Task 1.6 — Create `GetInventoryDashboardQuery` + Handler + DTOs

```
New: src/NOIR.Application/Features/Dashboard/DTOs/InventoryDashboardDtos.cs
New: src/NOIR.Application/Features/Dashboard/Queries/GetInventoryDashboard/GetInventoryDashboardQuery.cs
New: src/NOIR.Application/Features/Dashboard/Queries/GetInventoryDashboard/GetInventoryDashboardQueryHandler.cs
```

- [ ] `InventoryDashboardDto`: low stock alerts, recent receipts, value summary, movement trend
- [ ] Handler: Query `ProductVariant` where `StockQuantity <= threshold`
- [ ] Handler: Recent `InventoryReceipt` entries
- [ ] Handler: Aggregate stock values and counts

#### Task 1.7 — Add dashboard endpoints

```
Modify: src/NOIR.Web/Endpoints/DashboardEndpoints.cs
```

- [ ] `GET /api/dashboard/core` → `GetCoreDashboardQuery`
- [ ] `GET /api/dashboard/ecommerce` → `GetEcommerceDashboardQuery`
- [ ] `GET /api/dashboard/blog` → `GetBlogDashboardQuery`
- [ ] `GET /api/dashboard/inventory` → `GetInventoryDashboardQuery`
- [ ] All require `Permissions.DashboardRead`
- [ ] Ecommerce: `.RequireFeature(ModuleNames.Ecommerce.Orders)`
- [ ] Blog: `.RequireFeature(ModuleNames.Content.Blog)`
- [ ] Inventory: `.RequireFeature(ModuleNames.Ecommerce.Inventory)`

**Depends on:** Tasks 1.3–1.6

#### Task 1.8 — Backend unit tests

```
New: tests/NOIR.Application.UnitTests/Features/Dashboard/GetCoreDashboardQueryHandlerTests.cs
New: tests/NOIR.Application.UnitTests/Features/Dashboard/GetBlogDashboardQueryHandlerTests.cs
New: tests/NOIR.Application.UnitTests/Features/Dashboard/GetInventoryDashboardQueryHandlerTests.cs
```

- [ ] ~8 tests per handler (success, empty, edge cases, platform admin vs tenant admin)
- [ ] Follow pattern: `Handle_{Scenario}_Should{Result}()`
- [ ] Mock repositories, verify `TagWith()` usage

**Depends on:** Tasks 1.3–1.6

#### Task 1.9 — Integration tests

```
Modify: tests/NOIR.IntegrationTests/Endpoints/DashboardEndpointsTests.cs
```

- [ ] Test: `GetCoreDashboard_AsAdmin_ShouldReturn200()`
- [ ] Test: `GetEcommerceDashboard_AsAdmin_ShouldReturn200()`
- [ ] Test: `GetBlogDashboard_AsAdmin_ShouldReturn200()`
- [ ] Test: `GetInventoryDashboard_AsAdmin_ShouldReturn200()`
- [ ] Test: `GetCoreDashboard_Unauthenticated_ShouldReturn401()`
- [ ] Test: Feature-gated endpoint returns 403 when module disabled

**Depends on:** Task 1.7

### Stage 1B: Frontend (frontend-dev) — Can start after Task 1.7

#### Task 1.10 — Extend dashboard service + hooks

```
Modify: src/NOIR.Web/frontend/src/services/dashboard.ts
Modify: src/NOIR.Web/frontend/src/hooks/useDashboard.ts
```

- [ ] Add `getCoreDashboard()`, `getEcommerceDashboard()`, `getBlogDashboard()`, `getInventoryDashboard()`
- [ ] Add TypeScript interfaces for all new DTOs
- [ ] Add `useCoreDashboard()`, `useEcommerceDashboard()`, `useBlogDashboard()`, `useInventoryDashboard()`
- [ ] Query keys: `dashboardKeys.core()`, `.ecommerce()`, `.blog()`, `.inventory()`

#### Task 1.11 — Create widget components

```
New: src/NOIR.Web/frontend/src/portal-app/dashboard/features/dashboard/components/widgets/WelcomeCard.tsx
New: .../QuickActionsCard.tsx
New: .../ActivityFeed.tsx
New: .../SystemHealthCard.tsx
New: .../RevenueOverviewCard.tsx
New: .../RevenueChart.tsx         (recharts AreaChart — copy pattern from ReportsPage)
New: .../OrderMetricsCard.tsx
New: .../OrderStatusChart.tsx     (recharts BarChart)
New: .../CustomerMetricsCard.tsx
New: .../ProductPerformanceCard.tsx
New: .../BlogStatsCard.tsx
New: .../PublishingTrendChart.tsx
New: .../LowStockAlertsCard.tsx
New: .../RecentReceiptsCard.tsx
New: .../InventoryValueCard.tsx
New: .../DashboardSkeleton.tsx
```

- [ ] Each widget: Card from `@uikit` + data display + loading skeleton
- [ ] Charts: Use `hsl(var(--chart-N))` CSS variables (from ReportsPage pattern)
- [ ] Tooltip styling: Copy `CHART_TOOLTIP_STYLE` constant from ReportsPage
- [ ] All text via `t('dashboard.*')` keys
- [ ] `cursor-pointer` on clickable elements (links to detail pages)

**Depends on:** Task 1.10 (hooks)

#### Task 1.12 — Create widget group components

```
New: .../components/CoreWidgetGroup.tsx
New: .../components/EcommerceWidgetGroup.tsx
New: .../components/BlogWidgetGroup.tsx
New: .../components/InventoryWidgetGroup.tsx
```

- [ ] Each group fetches its own data via dedicated hook
- [ ] Grid layout within each group for child widgets
- [ ] Error boundaries per group (one group failing doesn't crash others)

**Depends on:** Task 1.11 (widgets)

#### Task 1.13 — Replace DashboardPage placeholder

```
Modify: src/NOIR.Web/frontend/src/portal-app/dashboard/features/dashboard/DashboardPage.tsx
```

- [ ] Remove `EmptyState` placeholder
- [ ] Add `useFeatures()` to check module availability
- [ ] Conditional rendering: `isEcommerceEnabled`, `isBlogEnabled`, `isInventoryEnabled`
- [ ] `Suspense` wrappers for lazy-loaded groups
- [ ] CSS Grid: `grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6`

**Depends on:** Task 1.12 (groups)

#### Task 1.14 — Localization

```
Modify: src/NOIR.Web/frontend/public/locales/en/common.json
Modify: src/NOIR.Web/frontend/public/locales/vi/common.json
```

- [ ] Add ~40 dashboard keys (quickActions, metrics, charts, empty states)
- [ ] Both EN and VI simultaneously

### Quality Gate 1

```bash
dotnet build src/NOIR.sln              # 0 errors
dotnet test src/NOIR.sln               # ALL pass (including new tests)
cd src/NOIR.Web/frontend && pnpm run build  # 0 errors
cd src/NOIR.Web/frontend && pnpm build-storybook  # 0 errors
```

**Checkpoint:** Commit `feat(dashboard): implement modular widget dashboard with 4 widget groups`

---

## Phase 2: Media Manager

**Priority:** High
**Risk:** Medium (CQRS refactor of inline code)
**Team:** `backend-dev` + `frontend-dev`
**Depends on:** Phase 0 (bug fixes)

### Stage 2A: Backend (backend-dev)

#### Task 2.1 — Create Media feature directory + DTOs

```
New: src/NOIR.Application/Features/Media/DTOs/MediaDtos.cs
```

- [ ] `MediaFileDto`, `MediaVariantDto` records
- [ ] `MediaFilesParams` for query parameters

#### Task 2.2 — Create Media specifications

```
New: src/NOIR.Application/Features/Media/Specifications/MediaSpecifications.cs
```

- [ ] `MediaFilesFilteredSpec` — search, type filter, folder filter, sort, pagination
- [ ] `MediaFileByIdSpec` — single file lookup
- [ ] `MediaFilesByIdsForUpdateSpec` — bulk operations with `.AsTracking()`
- [ ] All specs use `.TagWith("Media_*")`

#### Task 2.3 — Create `GetMediaFilesQuery` + Handler

```
New: src/NOIR.Application/Features/Media/Queries/GetMediaFiles/GetMediaFilesQuery.cs
New: src/NOIR.Application/Features/Media/Queries/GetMediaFiles/GetMediaFilesQueryHandler.cs
```

- [ ] Paginated query with search, type filter, folder filter, sort
- [ ] Returns `PaginatedResult<MediaFileDto>`
- [ ] Deserialize `VariantsJson` to `MediaVariantDto[]` in mapping

#### Task 2.4 — Create `GetMediaFileByIdQuery` + Handler

```
New: src/NOIR.Application/Features/Media/Queries/GetMediaFileById/GetMediaFileByIdQuery.cs
New: src/NOIR.Application/Features/Media/Queries/GetMediaFileById/GetMediaFileByIdQueryHandler.cs
```

#### Task 2.5 — Refactor upload to CQRS

```
New: src/NOIR.Application/Features/Media/Commands/UploadMediaFile/UploadMediaFileCommand.cs
New: src/NOIR.Application/Features/Media/Commands/UploadMediaFile/UploadMediaFileCommandHandler.cs
New: src/NOIR.Application/Features/Media/Commands/UploadMediaFile/UploadMediaFileCommandValidator.cs
Modify: src/NOIR.Web/Endpoints/MediaEndpoints.cs  (extract inline logic to handler)
```

- [ ] Extract upload logic from `MediaEndpoints.cs` lines 18-195 into handler
- [ ] MediaEndpoints becomes thin — just maps HTTP to command
- [ ] Validator: file size, format, folder validation
- [ ] Handler: delegates to `IImageProcessor.ProcessAsync()`, creates `MediaFile` entity

**CAUTION:** This is the riskiest task — existing upload must keep working. Test thoroughly.

#### Task 2.6 — Create Delete + Rename + BulkDelete commands

```
New: src/NOIR.Application/Features/Media/Commands/DeleteMediaFile/DeleteMediaFileCommand.cs
New: src/NOIR.Application/Features/Media/Commands/DeleteMediaFile/DeleteMediaFileCommandHandler.cs
New: src/NOIR.Application/Features/Media/Commands/RenameMediaFile/RenameMediaFileCommand.cs
New: src/NOIR.Application/Features/Media/Commands/RenameMediaFile/RenameMediaFileCommandHandler.cs
New: src/NOIR.Application/Features/Media/Commands/RenameMediaFile/RenameMediaFileCommandValidator.cs
New: src/NOIR.Application/Features/Media/Commands/BulkDeleteMediaFiles/BulkDeleteMediaFilesCommand.cs
New: src/NOIR.Application/Features/Media/Commands/BulkDeleteMediaFiles/BulkDeleteMediaFilesCommandHandler.cs
```

- [ ] Delete: Soft delete via repository `.Remove()`, fires `MediaFileDeletedEvent`
- [ ] Rename: Update `OriginalFileName`, regenerate slug
- [ ] BulkDelete: Batch load with `AsTracking()`, iterate + soft delete

#### Task 2.7 — Wire domain event handlers

```
New: src/NOIR.Application/Features/Media/EventHandlers/MediaFileUploadedHandler.cs
New: src/NOIR.Application/Features/Media/EventHandlers/MediaFileDeletedHandler.cs
```

- [ ] `MediaFileUploadedHandler`: Post-processing notifications (optional, can be no-op initially)
- [ ] `MediaFileDeletedHandler`: Cleanup storage files on soft-delete

#### Task 2.8 — Extend MediaEndpoints

```
Modify: src/NOIR.Web/Endpoints/MediaEndpoints.cs
```

- [ ] `GET /api/media` → `GetMediaFilesQuery`
- [ ] `PUT /api/media/{id}/rename` → `RenameMediaFileCommand`
- [ ] `DELETE /api/media/{id}` → `DeleteMediaFileCommand`
- [ ] `POST /api/media/bulk-delete` → `BulkDeleteMediaFilesCommand`
- [ ] Keep existing upload/query endpoints

#### Task 2.9 — Backend tests

```
New: tests/NOIR.Application.UnitTests/Features/Media/GetMediaFilesQueryHandlerTests.cs
New: tests/NOIR.Application.UnitTests/Features/Media/UploadMediaFileCommandHandlerTests.cs
New: tests/NOIR.Application.UnitTests/Features/Media/DeleteMediaFileCommandHandlerTests.cs
New: tests/NOIR.Application.UnitTests/Features/Media/RenameMediaFileCommandHandlerTests.cs
New: tests/NOIR.IntegrationTests/Endpoints/MediaEndpointsTests.cs
```

### Stage 2B: Frontend (frontend-dev) — Can start after Task 2.8

#### Task 2.10 — Extend media service + create hook

```
Modify: src/NOIR.Web/frontend/src/services/media.ts
New: src/NOIR.Web/frontend/src/hooks/useMediaFiles.ts
```

- [ ] Add `getMediaFiles(params)`, `deleteMediaFile(id)`, `renameMediaFile(id, name)`, `bulkDeleteMediaFiles(ids)`
- [ ] `useMediaFiles()` hook with `keepPreviousData` for smooth pagination
- [ ] `useDeleteMediaFile()`, `useRenameMediaFile()` mutation hooks

#### Task 2.11 — Create MediaLibraryPage + components

```
New: src/NOIR.Web/frontend/src/portal-app/media/features/media-library/MediaLibraryPage.tsx
New: .../components/MediaGrid.tsx
New: .../components/MediaList.tsx
New: .../components/MediaToolbar.tsx
New: .../components/MediaUploadZone.tsx
New: .../components/MediaDetailSheet.tsx
```

- [ ] Grid/List toggle (localStorage preference)
- [ ] Search, type filter, folder filter, sort
- [ ] ThumbHash placeholders for progressive loading
- [ ] Click-to-preview with existing `FilePreviewModal`
- [ ] Drag-drop upload zone at top
- [ ] Side sheet for file details (dimensions, URL, copy-to-clipboard)
- [ ] Bulk select + delete toolbar

#### Task 2.12 — Create MediaPickerDialog

```
New: src/NOIR.Web/frontend/src/components/media/MediaPickerDialog.tsx
```

- [ ] Reusable dialog wrapping MediaGrid + MediaUploadZone
- [ ] Single select mode (default) + multi-select mode
- [ ] Folder pre-filter prop
- [ ] MIME type filter prop
- [ ] "Upload new" inline capability

#### Task 2.13 — Add route + navigation

```
Modify: src/NOIR.Web/frontend/src/App.tsx (add route)
Modify: sidebar navigation (add Media link)
Modify: command palette navigation items
```

#### Task 2.14 — Localization

```
Modify: src/NOIR.Web/frontend/public/locales/en/common.json
Modify: src/NOIR.Web/frontend/public/locales/vi/common.json
```

- [ ] ~30 media keys (library, upload, filters, details, actions)

### Quality Gate 2

```bash
dotnet build src/NOIR.sln              # 0 errors
dotnet test src/NOIR.sln               # ALL pass
cd src/NOIR.Web/frontend && pnpm run build  # 0 errors
cd src/NOIR.Web/frontend && pnpm build-storybook  # 0 errors
```

**Verify:** Existing upload flows (product images, blog images, avatars) still work correctly.

**Checkpoint:** Commit `feat(media): add media library page with CQRS commands and MediaPickerDialog`

---

## Phase 3: Global Search

**Priority:** Medium
**Risk:** Low (leverages existing search specs)
**Team:** `backend-dev` + `frontend-dev`

### Stage 3A: Backend (backend-dev)

#### Task 3.1 — Create Search DTOs

```
New: src/NOIR.Application/Features/Search/DTOs/SearchDtos.cs
```

- [ ] `GlobalSearchResultDto`, `SearchResultGroupDto`, `SearchResultItemDto`

#### Task 3.2 — Create search specifications (per entity)

```
New: src/NOIR.Application/Features/Search/Specifications/SearchSpecifications.cs
```

- [ ] `ProductsGlobalSearchSpec(string query, int take)` — Name, SKU, Description
- [ ] `OrdersGlobalSearchSpec(string query, int take)` — OrderNumber, CustomerName, Email
- [ ] `CustomersGlobalSearchSpec(string query, int take)` — Name, Email, Phone
- [ ] `PostsGlobalSearchSpec(string query, int take)` — Title, content excerpt
- [ ] `UsersGlobalSearchSpec(string query, int take)` — Name, Email
- [ ] All use `.AsNoTracking().TagWith("GlobalSearch_*")`

#### Task 3.3 — Create `GlobalSearchQuery` + Handler + Validator

```
New: src/NOIR.Application/Features/Search/Queries/GlobalSearch/GlobalSearchQuery.cs
New: src/NOIR.Application/Features/Search/Queries/GlobalSearch/GlobalSearchQueryHandler.cs
New: src/NOIR.Application/Features/Search/Queries/GlobalSearch/GlobalSearchQueryValidator.cs
```

- [ ] Validator: `Query.Length >= 2`, `MaxPerType <= 10`
- [ ] Handler: Check `IFeatureChecker` per entity type
- [ ] Handler: `Task.WhenAll()` parallel search across enabled entity types
- [ ] Handler: Return null groups for empty results (filter out)
- [ ] Map to `SearchResultItemDto` with frontend route URLs

#### Task 3.4 — Create SearchEndpoints

```
New: src/NOIR.Web/Endpoints/SearchEndpoints.cs
```

- [ ] `GET /api/search?q=keyword&types=products,orders&maxPerType=5`
- [ ] Requires authentication (any authenticated user)
- [ ] No specific permission (results filtered by feature gates)

#### Task 3.5 — Backend tests

```
New: tests/NOIR.Application.UnitTests/Features/Search/GlobalSearchQueryHandlerTests.cs
New: tests/NOIR.IntegrationTests/Endpoints/SearchEndpointsTests.cs
```

- [ ] Test: Search products returns results
- [ ] Test: Empty query returns validation error
- [ ] Test: Disabled module returns no results for that type
- [ ] Test: `Task.WhenAll()` parallel execution
- [ ] Integration: endpoint returns grouped results

### Stage 3B: Frontend (frontend-dev) — Can start after Task 3.4

#### Task 3.6 — Add search service

```
Modify: src/NOIR.Web/frontend/src/services/search.ts (new file or extend existing)
```

- [ ] `globalSearch(query, types?, maxPerType?)` function
- [ ] TypeScript interfaces matching backend DTOs

#### Task 3.7 — Refactor CommandPalette into tabs

```
Modify: src/NOIR.Web/frontend/src/components/command-palette/CommandPalette.tsx
New: .../tabs/PagesTab.tsx     (extract existing navigation)
New: .../tabs/SearchTab.tsx    (new content search)
New: .../tabs/RecentTab.tsx    (extract existing recent pages)
New: .../tabs/ActionsTab.tsx   (extract existing quick actions)
```

- [ ] Extract existing sections into tab components
- [ ] Add tab bar: Pages | Search | Recent | Actions
- [ ] SearchTab: debounced input with `useDeferredValue`
- [ ] SearchTab: min 2 chars, loading skeletons per group

#### Task 3.8 — Create search result components

```
New: .../SearchResultGroup.tsx
New: .../SearchResultItem.tsx
```

- [ ] Group header with type icon + count
- [ ] Item: title, subtitle, optional image thumbnail
- [ ] "See all" link per group → navigates to entity list with `?search=keyword`
- [ ] Click item → navigate to entity detail + close palette

#### Task 3.9 — Localization

```
Modify: src/NOIR.Web/frontend/public/locales/en/common.json
Modify: src/NOIR.Web/frontend/public/locales/vi/common.json
```

- [ ] ~15 search keys (placeholder, no results, group labels, see all)

### Quality Gate 3

```bash
dotnet build src/NOIR.sln              # 0 errors
dotnet test src/NOIR.sln               # ALL pass
cd src/NOIR.Web/frontend && pnpm run build  # 0 errors
```

**Checkpoint:** Commit `feat(search): add global search with command palette integration`

---

## Phase 4: Import/Export UI

**Priority:** Medium
**Risk:** Medium (new libraries, SSE integration)
**Team:** `backend-dev` + `frontend-dev`

### Stage 4A: Backend (backend-dev)

#### Task 4.1 — Install ClosedXML

```
Modify: src/NOIR.Infrastructure/NOIR.Infrastructure.csproj
```

```bash
dotnet add src/NOIR.Infrastructure package ClosedXML
```

- [ ] Verify build succeeds after install

#### Task 4.2 — Create `IExportService` + implementation

```
New: src/NOIR.Application/Common/Interfaces/IExportService.cs
New: src/NOIR.Infrastructure/Services/ExportService.cs
```

- [ ] `GenerateCsv<T>(data, columns)` → byte[] with BOM
- [ ] `GenerateExcel<T>(data, columns, sheetName)` → byte[] (ClosedXML)
- [ ] `ExportColumnDefinition` record with header, property name, optional formatter
- [ ] Register as `IScopedService`

#### Task 4.3 — Fix Excel export stub in ReportQueryService

```
Modify: src/NOIR.Infrastructure/Services/ReportQueryService.cs
```

- [ ] Replace CSV-only logic with `IExportService` delegation
- [ ] `ExportFormat.Excel` → `GenerateExcel()`
- [ ] `ExportFormat.Csv` → `GenerateCsv()`
- [ ] Keep existing report data queries unchanged

#### Task 4.4 — Create Customer export/import commands

```
New: src/NOIR.Application/Features/Customers/Queries/ExportCustomers/ExportCustomersQuery.cs
New: src/NOIR.Application/Features/Customers/Queries/ExportCustomers/ExportCustomersQueryHandler.cs
New: src/NOIR.Application/Features/Customers/Commands/ImportCustomers/ImportCustomersCommand.cs
New: src/NOIR.Application/Features/Customers/Commands/ImportCustomers/ImportCustomersCommandHandler.cs
New: src/NOIR.Application/Features/Customers/Commands/ImportCustomers/ImportCustomersCommandValidator.cs
```

- [ ] Export: query customers with filters → `IExportService.Generate{Format}()`
- [ ] Import: follow `BulkImportProductsCommandHandler` pattern
- [ ] Import: pre-load customer groups, validate email uniqueness
- [ ] Import: duplicate detection by email
- [ ] Return `BulkImportResultDto` (success/failed/errors)

#### Task 4.5 — Create Order export query

```
New: src/NOIR.Application/Features/Orders/Queries/ExportOrders/ExportOrdersQuery.cs
New: src/NOIR.Application/Features/Orders/Queries/ExportOrders/ExportOrdersQueryHandler.cs
```

- [ ] Export only (no import for orders)
- [ ] Include: order number, date, customer, status, total, items summary

#### Task 4.6 — Create `ImportHistory` entity

```
New: src/NOIR.Domain/Entities/ImportHistory.cs
New: src/NOIR.Infrastructure/Persistence/Configurations/ImportHistoryConfiguration.cs
```

- [ ] Entity: EntityType, FileName, TotalRows, SuccessCount, FailedCount, ErrorReportUrl, ImportedBy, ImportedAt
- [ ] EF config: indexes on TenantId + EntityType, TenantId + ImportedAt

#### Task 4.7 — Create EF migration

```bash
dotnet ef migrations add AddImportHistory --project src/NOIR.Infrastructure --startup-project src/NOIR.Web --context ApplicationDbContext --output-dir Migrations/App
```

- [ ] Verify migration SQL is correct
- [ ] Update database

#### Task 4.8 — Extend endpoints

```
Modify: src/NOIR.Web/Endpoints/CustomerEndpoints.cs
Modify: src/NOIR.Web/Endpoints/OrderEndpoints.cs
```

- [ ] `GET /api/customers/export?format=csv&search=...` → `ExportCustomersQuery`
- [ ] `POST /api/customers/import` → `ImportCustomersCommand`
- [ ] `GET /api/orders/export?format=csv&...` → `ExportOrdersQuery`

#### Task 4.9 — Backend tests

```
New: tests/NOIR.Application.UnitTests/Services/ExportServiceTests.cs
New: tests/NOIR.Application.UnitTests/Features/Customers/ExportCustomersQueryHandlerTests.cs
New: tests/NOIR.Application.UnitTests/Features/Customers/ImportCustomersCommandHandlerTests.cs
New: tests/NOIR.IntegrationTests/Endpoints/CustomerExportImportTests.cs
```

### Stage 4B: Frontend (frontend-dev) — Can start after Task 4.8

#### Task 4.10 — Install SheetJS

```bash
cd src/NOIR.Web/frontend && pnpm add xlsx
```

#### Task 4.11 — Create ExportDialog

```
New: src/NOIR.Web/frontend/src/components/import-export/ExportDialog.tsx
```

- [ ] Format selector: CSV / Excel (XLSX)
- [ ] Column checkboxes (select which fields to export)
- [ ] "Export filtered" option (passes current filters)
- [ ] Download trigger on completion
- [ ] Use `Credenza` dialog (per design standards)

#### Task 4.12 — Create ImportWizard

```
New: src/NOIR.Web/frontend/src/components/import-export/ImportWizard.tsx
New: .../ImportWizardSteps/UploadStep.tsx
New: .../ImportWizardSteps/ColumnMappingStep.tsx
New: .../ImportWizardSteps/PreviewStep.tsx
New: .../ImportWizardSteps/ExecuteStep.tsx
New: .../ImportWizardSteps/SummaryStep.tsx
```

- [ ] Step 1: Upload CSV/Excel file, parse with SheetJS
- [ ] Step 2: Map file columns to entity fields (drag-drop or dropdowns)
- [ ] Step 3: Preview first 10 rows, show validation errors
- [ ] Step 4: Execute import, SSE progress bar
- [ ] Step 5: Summary with success/error counts, download error report

#### Task 4.13 — Create ImportHistoryDialog

```
New: src/NOIR.Web/frontend/src/components/import-export/ImportHistoryDialog.tsx
```

- [ ] List past imports with date, user, file, result counts
- [ ] Re-download imported file link

#### Task 4.14 — Add export/import to list pages

```
Modify: ProductsPage.tsx (add ExportDialog, keep existing import)
Modify: CustomersPage.tsx (add ExportDialog + ImportWizard)
Modify: OrdersPage.tsx (add ExportDialog)
```

#### Task 4.15 — Localization

```
Modify: src/NOIR.Web/frontend/public/locales/en/common.json
Modify: src/NOIR.Web/frontend/public/locales/vi/common.json
```

- [ ] ~30 import/export keys (steps, labels, buttons, errors, success messages)

### Quality Gate 4

```bash
dotnet build src/NOIR.sln              # 0 errors
dotnet test src/NOIR.sln               # ALL pass
cd src/NOIR.Web/frontend && pnpm run build  # 0 errors
```

**Verify:** Existing product import/export still works. Reports Excel export now generates real XLSX.

**Checkpoint:** Commit `feat(import-export): add Excel export, customer import/export, ImportWizard component`

---

## Phase 5: Bulk Actions Enhancement

**Priority:** Low
**Risk:** Low (pattern established)
**Team:** `backend-dev` + `frontend-dev`

### Stage 5A: Shared Frontend Infrastructure (frontend-dev)

#### Task 5.1 — Create `useBulkSelection` hook

```
New: src/NOIR.Web/frontend/src/hooks/useBulkSelection.ts
```

- [ ] Generic `<T>` with `items` and `getId` parameters
- [ ] Returns: `selectedIds`, `selectedCount`, `isAllSelected`, `isPartiallySelected`, `toggleSelect`, `selectAll`, `selectNone`, `isSelected`
- [ ] Resets selection on items change (pagination/filter)

#### Task 5.2 — Create shared bulk components

```
New: src/NOIR.Web/frontend/src/components/bulk-actions/BulkActionToolbar.tsx
New: .../BulkCheckboxCell.tsx
New: .../BulkConfirmDialog.tsx
```

- [ ] `BulkActionToolbar`: configurable actions array, selected count badge, clear button
- [ ] Style: `bg-primary/5 border border-primary/20 rounded-lg` (match existing)
- [ ] `BulkConfirmDialog`: destructive variant, item count, confirm/cancel
- [ ] `cursor-pointer` on all interactive elements

#### Task 5.3 — Migrate ProductsPage to shared components

```
Modify: src/NOIR.Web/frontend/src/portal-app/products/features/product-list/ProductsPage.tsx
```

- [ ] Replace inline `useState<Set<string>>` with `useBulkSelection()`
- [ ] Replace inline toolbar JSX with `<BulkActionToolbar />`
- [ ] **NO behavior change** — pure refactor
- [ ] Verify: bulk publish/archive/delete still work

#### Task 5.4 — Migrate ReviewsPage to shared components

```
Modify: src/NOIR.Web/frontend/src/portal-app/reviews/features/review-list/ReviewsPage.tsx
```

- [ ] Same migration as ProductsPage
- [ ] Verify: bulk approve/reject still work

### Stage 5B: Backend (backend-dev) — Can run parallel to Stage 5A

#### Task 5.5 — Extract shared bulk constant

```
New or Modify: src/NOIR.Domain/Common/BulkConstants.cs (or add to existing)
```

- [ ] `public const int MaxBulkOperationSize = 1000;` — shared across all entities
- [ ] Update `ProductConstants.MaxBulkOperationSize` to reference shared constant (or keep both)

#### Task 5.6 — Create Order bulk commands

```
New: src/NOIR.Application/Features/Orders/Commands/BulkUpdateOrderStatus/BulkUpdateOrderStatusCommand.cs
New: .../BulkUpdateOrderStatusCommandHandler.cs
New: .../BulkUpdateOrderStatusCommandValidator.cs
```

- [ ] Validate: target status is valid transition from current status
- [ ] Per-item error collection
- [ ] `IAuditableCommand` implementation

#### Task 5.7 — Create Customer bulk commands

```
New: src/NOIR.Application/Features/CustomerGroups/Commands/BulkAddToGroup/BulkAddToGroupCommand.cs
New: .../BulkAddToGroupCommandHandler.cs
New: .../BulkAddToGroupCommandValidator.cs
New: .../BulkRemoveFromGroup/BulkRemoveFromGroupCommand.cs
New: .../BulkRemoveFromGroupCommandHandler.cs
New: .../BulkRemoveFromGroupCommandValidator.cs
```

#### Task 5.8 — Create Blog Post bulk commands

```
New: src/NOIR.Application/Features/Blog/Commands/BulkPublishPosts/BulkPublishPostsCommand.cs
New: .../BulkPublishPostsCommandHandler.cs
New: .../BulkPublishPostsCommandValidator.cs
New: .../BulkUnpublishPosts/BulkUnpublishPostsCommand.cs
New: .../BulkUnpublishPostsCommandHandler.cs
New: .../BulkUnpublishPostsCommandValidator.cs
New: .../BulkDeletePosts/BulkDeletePostsCommand.cs
New: .../BulkDeletePostsCommandHandler.cs
New: .../BulkDeletePostsCommandValidator.cs
```

#### Task 5.9 — Add bulk endpoints

```
Modify: src/NOIR.Web/Endpoints/OrderEndpoints.cs
Modify: src/NOIR.Web/Endpoints/CustomerGroupEndpoints.cs (or CustomerEndpoints)
Modify: src/NOIR.Web/Endpoints/BlogEndpoints.cs (or PostEndpoints)
```

- [ ] `POST /api/orders/bulk-update-status`
- [ ] `POST /api/customer-groups/bulk-add`
- [ ] `POST /api/customer-groups/bulk-remove`
- [ ] `POST /api/posts/bulk-publish`
- [ ] `POST /api/posts/bulk-unpublish`
- [ ] `POST /api/posts/bulk-delete`

#### Task 5.10 — Backend tests

```
New: tests/NOIR.Application.UnitTests/Features/Orders/BulkUpdateOrderStatusCommandHandlerTests.cs
New: tests/NOIR.Application.UnitTests/Features/CustomerGroups/BulkAddToGroupCommandHandlerTests.cs
New: tests/NOIR.Application.UnitTests/Features/CustomerGroups/BulkRemoveFromGroupCommandHandlerTests.cs
New: tests/NOIR.Application.UnitTests/Features/Blog/BulkPublishPostsCommandHandlerTests.cs
New: tests/NOIR.Application.UnitTests/Features/Blog/BulkUnpublishPostsCommandHandlerTests.cs
New: tests/NOIR.Application.UnitTests/Features/Blog/BulkDeletePostsCommandHandlerTests.cs
New: tests/NOIR.IntegrationTests/Endpoints/BulkOperationEndpointsTests.cs
```

- [ ] ~4 tests per handler (success, validation error, per-item errors, empty list)

### Stage 5C: Frontend Integration (after 5A + 5B)

#### Task 5.11 — Add bulk mutations + services

```
Modify: src/NOIR.Web/frontend/src/services/orders.ts
Modify: src/NOIR.Web/frontend/src/services/customers.ts (or customerGroups.ts)
Modify: src/NOIR.Web/frontend/src/services/blog.ts (or posts.ts)
```

- [ ] API functions for each new bulk endpoint
- [ ] Mutation hooks with query invalidation

#### Task 5.12 — Add bulk actions to remaining pages

```
Modify: OrdersPage.tsx — add useBulkSelection + BulkActionToolbar
Modify: CustomersPage.tsx — add useBulkSelection + BulkActionToolbar
Modify: BlogPostsPage.tsx — add useBulkSelection + BulkActionToolbar
```

- [ ] Orders: Change status bulk action
- [ ] Customers: Add to group, Remove from group
- [ ] Blog Posts: Publish, Unpublish, Delete

#### Task 5.13 — Localization

```
Modify: src/NOIR.Web/frontend/public/locales/en/common.json
Modify: src/NOIR.Web/frontend/public/locales/vi/common.json
```

- [ ] ~20 bulk action keys (confirm messages, result messages, toolbar labels)

### Quality Gate 5

```bash
dotnet build src/NOIR.sln              # 0 errors
dotnet test src/NOIR.sln               # ALL pass
cd src/NOIR.Web/frontend && pnpm run build  # 0 errors
cd src/NOIR.Web/frontend && pnpm build-storybook  # 0 errors
```

**Verify:** Existing Products + Reviews bulk actions still work after migration to shared components.

**Checkpoint:** Commit `feat(bulk-actions): add shared useBulkSelection hook and 6 new bulk commands`

---

## Final Quality Gate

After all phases complete:

```bash
# Full backend verification
dotnet build src/NOIR.sln              # 0 errors
dotnet test src/NOIR.sln               # ALL pass (11,341+ existing + ~100 new)

# Full frontend verification
cd src/NOIR.Web/frontend
pnpm run build                         # 0 errors, 0 warnings
pnpm build-storybook                   # 0 errors

# Manual verification checklist
# [ ] Dashboard shows widget groups based on enabled modules
# [ ] Media library lists, uploads, deletes, renames files
# [ ] Cmd+K search finds products, orders, customers
# [ ] CSV + Excel export works for Products, Customers, Orders
# [ ] Customer import wizard completes successfully
# [ ] Bulk actions work on Products, Reviews, Orders, Customers, Blog Posts
```

---

## Dependency Graph

```
Phase 0 ─────────────────────────────────────────────────────────→
  │                                                    (3 files)
  │
Phase 1 ─── 1A: Backend ──────→ 1B: Frontend ────────────────────→
  │        (Tasks 1.1-1.9)     (Tasks 1.10-1.14)      (~42 files)
  │                 ↑ depends
  │                 │
Phase 2 ─── 2A: Backend ──────→ 2B: Frontend ────────────────────→
  │        (Tasks 2.1-2.9)     (Tasks 2.10-2.14)      (~41 files)
  │         depends on Phase 0
  │
Phase 3 ─── 3A: Backend ──────→ 3B: Frontend ────────────────────→
  │        (Tasks 3.1-3.5)     (Tasks 3.6-3.9)        (~25 files)
  │
Phase 4 ─── 4A: Backend ──────→ 4B: Frontend ────────────────────→
  │        (Tasks 4.1-4.9)     (Tasks 4.10-4.15)      (~41 files)
  │
Phase 5 ─── 5A: FE Shared ────┐
  │        (Tasks 5.1-5.4)     │
  │                            ├─→ 5C: Integration ──────────────→
  │   5B: Backend ─────────────┘   (Tasks 5.11-5.13)  (~45 files)
  │        (Tasks 5.5-5.10)
```

**Parallelizable phases:** Phase 1 + Phase 3 can run in parallel (no dependencies). Phase 4 + Phase 5 can run in parallel.

---

## Team Assignment Reference

| Phase | backend-dev | frontend-dev | test-writer |
|-------|------------|-------------|------------|
| 0 | Bug fixes | — | Bug fix tests |
| 1 | Queries + endpoints | Widget components + page | Handler tests |
| 2 | CQRS refactor + endpoints | MediaLibraryPage + picker | Media tests |
| 3 | GlobalSearchQuery + endpoint | CommandPalette tabs | Search tests |
| 4 | IExportService + commands | ExportDialog + ImportWizard | Export tests |
| 5 | Bulk commands + endpoints | Shared hook + components | Bulk tests |

---

> **Next step:** Use `/sc:implement` per phase. Start with Phase 0 (3 bug fixes).
