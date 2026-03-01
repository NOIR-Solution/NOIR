# Admin Portal Enhancement v28 Feb - Technical Design

> Generated: 2026-02-28. Comprehensive technical design for 5 features.
> Source: [admin-portal-enhancement-v28-feb.md](admin-portal-enhancement-v28-feb.md)

---

## Table of Contents

1. [Dashboard (Modular Widget Architecture)](#1-dashboard-modular-widget-architecture)
2. [Media Manager](#2-media-manager)
3. [Global Search](#3-global-search)
4. [Import/Export UI](#4-importexport-ui)
5. [Bulk Actions Enhancement](#5-bulk-actions-enhancement)
6. [Implementation Order & Dependencies](#6-implementation-order--dependencies)

---

## 1. Dashboard (Modular Widget Architecture)

### 1.1 Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│ DashboardPage (CSS Grid auto-fill)                              │
│                                                                 │
│  ┌──────────────────────┐  ┌──────────────────────┐            │
│  │  CoreWidgetGroup     │  │  EcommerceWidgetGroup │            │
│  │  (always on)         │  │  FeatureGuard gated   │            │
│  │  ┌────────────────┐  │  │  ┌────────────────┐  │            │
│  │  │ WelcomeCard    │  │  │  │ RevenueOverview│  │            │
│  │  │ QuickActions   │  │  │  │ RevenueChart   │  │            │
│  │  │ ActivityFeed   │  │  │  │ OrderMetrics   │  │            │
│  │  │ SystemHealth*  │  │  │  │ CustomerMetrics│  │            │
│  │  └────────────────┘  │  │  │ ProductPerf    │  │            │
│  └──────────────────────┘  │  └────────────────┘  │            │
│                            └──────────────────────┘            │
│  ┌──────────────────────┐  ┌──────────────────────┐            │
│  │  BlogWidgetGroup     │  │  InventoryWidgetGroup│            │
│  │  FeatureGuard gated  │  │  FeatureGuard gated  │            │
│  └──────────────────────┘  └──────────────────────┘            │
│                                                                 │
│  * SystemHealth = platform admin only                           │
└─────────────────────────────────────────────────────────────────┘
```

### 1.2 Backend Changes

#### New Module Sub-Features

Add dashboard sub-features to `ModuleNames`:

```
File: src/NOIR.Application/Modules/ModuleNames.cs
```

```csharp
public static class Dashboard
{
    public const string Core = "Dashboard.Core";           // Always enabled (core)
    public const string Ecommerce = "Dashboard.Ecommerce"; // Gated
    public const string Blog = "Dashboard.Blog";           // Gated
    public const string Inventory = "Dashboard.Inventory"; // Gated
}
```

These are NOT new module definitions. They are **sub-feature constants** used by the frontend `useFeatures()` check. The existing `DashboardModuleDefinition` (IsCore=true) gates the entire page; these sub-features gate individual widget groups within it.

**Decision point:** Sub-features can either be:
- **(A) Convention-only** — Frontend checks module availability (`Ecommerce.*` enabled → show Ecommerce dashboard). No new DB rows.
- **(B) Explicit sub-features** — Register in `DashboardModuleDefinition.Features` list. New `FeatureDefinition` rows, toggleable in `ModulesSettingsTab`.

**Recommendation: Option A.** Dashboard widget groups mirror existing module families. If `Ecommerce.Orders` is enabled, show ecommerce widgets. No extra DB config. Simpler, zero migration.

#### New Queries (one per widget group)

```
src/NOIR.Application/Features/Dashboard/
├── Queries/
│   ├── GetDashboardMetrics/          # Existing (keep for backward compat)
│   ├── GetCoreDashboard/
│   │   ├── GetCoreDashboardQuery.cs
│   │   └── GetCoreDashboardQueryHandler.cs
│   ├── GetEcommerceDashboard/
│   │   ├── GetEcommerceDashboardQuery.cs
│   │   └── GetEcommerceDashboardQueryHandler.cs
│   ├── GetBlogDashboard/
│   │   ├── GetBlogDashboardQuery.cs
│   │   └── GetBlogDashboardQueryHandler.cs
│   └── GetInventoryDashboard/
│       ├── GetInventoryDashboardQuery.cs
│       └── GetInventoryDashboardQueryHandler.cs
├── DTOs/
│   ├── DashboardDtos.cs              # Existing ecommerce DTOs
│   ├── CoreDashboardDtos.cs          # New
│   ├── BlogDashboardDtos.cs          # New
│   └── InventoryDashboardDtos.cs     # New
```

#### Query Specifications

**GetCoreDashboardQuery:**

```csharp
public sealed record GetCoreDashboardQuery(int ActivityCount = 10);
```

Returns:

```csharp
public sealed record CoreDashboardDto(
    QuickActionCountsDto QuickActions,
    IReadOnlyList<ActivityFeedItemDto> RecentActivity,
    SystemHealthDto? SystemHealth);  // null for tenant admins

public sealed record QuickActionCountsDto(
    int PendingOrders,
    int PendingReviews,
    int LowStockAlerts,
    int DraftProducts);

public sealed record ActivityFeedItemDto(
    string Type,        // "order", "registration", "review", "system"
    string Title,
    string Description,
    DateTimeOffset Timestamp,
    string? EntityId,
    string? EntityUrl);

public sealed record SystemHealthDto(
    bool ApiHealthy,
    int BackgroundJobsQueued,
    int BackgroundJobsFailed,
    int ActiveTenants);
```

**Handler logic:**
- `QuickActions`: 4 count queries via `Task.WhenAll()` — pending orders, pending reviews, low stock (threshold 10), draft products
- `RecentActivity`: Union query from `AuditLog` table (latest N entries), mapped to `ActivityFeedItemDto`
- `SystemHealth`: Only populated when `ICurrentUser.IsPlatformAdmin`. Reads Hangfire stats + tenant count.

**GetEcommerceDashboardQuery:**

```csharp
public sealed record GetEcommerceDashboardQuery(
    int TopProductsCount = 5,
    int LowStockThreshold = 10,
    int RecentOrdersCount = 10,
    int SalesOverTimeDays = 30);
```

Returns existing `DashboardMetricsDto`. Handler delegates to existing `IDashboardQueryService.GetMetricsAsync()`.

**GetBlogDashboardQuery:**

```csharp
public sealed record GetBlogDashboardQuery(int TrendDays = 30);
```

Returns:

```csharp
public sealed record BlogDashboardDto(
    int TotalPosts,
    int PublishedPosts,
    int DraftPosts,
    int ArchivedPosts,
    int PendingComments,         // If comment moderation exists
    IReadOnlyList<TopPostDto> TopPosts,
    IReadOnlyList<PublishingTrendDto> PublishingTrend);

public sealed record TopPostDto(Guid PostId, string Title, string? ImageUrl, int ViewCount);
public sealed record PublishingTrendDto(DateOnly Date, int PostCount);
```

**GetInventoryDashboardQuery:**

```csharp
public sealed record GetInventoryDashboardQuery(int LowStockThreshold = 10, int RecentReceiptsCount = 5);
```

Returns:

```csharp
public sealed record InventoryDashboardDto(
    IReadOnlyList<LowStockAlertDto> LowStockAlerts,
    IReadOnlyList<RecentReceiptDto> RecentReceipts,
    InventoryValueSummaryDto ValueSummary,
    IReadOnlyList<StockMovementTrendDto> StockMovementTrend);

public sealed record LowStockAlertDto(Guid ProductId, string ProductName, string? Sku, int CurrentStock, int Threshold);
public sealed record RecentReceiptDto(Guid ReceiptId, string ReceiptNumber, string Type, DateTimeOffset Date, int ItemCount);
public sealed record InventoryValueSummaryDto(decimal TotalValue, int TotalSku, int InStockSku, int OutOfStockSku);
public sealed record StockMovementTrendDto(DateOnly Date, int StockIn, int StockOut);
```

#### New Endpoints

```
File: src/NOIR.Web/Endpoints/DashboardEndpoints.cs
```

```
GET /api/dashboard/metrics          # Existing (keep)
GET /api/dashboard/core             # New → GetCoreDashboardQuery
GET /api/dashboard/ecommerce        # New → GetEcommerceDashboardQuery
GET /api/dashboard/blog             # New → GetBlogDashboardQuery
GET /api/dashboard/inventory        # New → GetInventoryDashboardQuery
```

Each new endpoint:
- Requires authentication
- Permission: `Permissions.DashboardRead` (new permission) — OR keep existing `Permissions.OrdersRead` for ecommerce, add granular later
- Feature-gated: `.RequireFeature(ModuleNames.Ecommerce.Orders)` for ecommerce, etc.
- Tags with `TagWith("Dashboard_{Group}")` for SQL logging

**Permission decision:** Create `Permissions.DashboardRead` (new) as a lightweight permission. All authenticated users with this permission see the core dashboard; module-specific widgets additionally require the module to be enabled.

### 1.3 Frontend Implementation

#### File Structure

```
src/NOIR.Web/frontend/src/portal-app/dashboard/
├── features/dashboard/
│   ├── DashboardPage.tsx                    # Main page (replace placeholder)
│   ├── components/
│   │   ├── CoreWidgetGroup.tsx              # Welcome, Quick Actions, Activity Feed
│   │   ├── EcommerceWidgetGroup.tsx         # Revenue, Orders, Products, Customers
│   │   ├── BlogWidgetGroup.tsx              # Posts, Comments, Trends
│   │   ├── InventoryWidgetGroup.tsx         # Stock alerts, Receipts, Value
│   │   ├── widgets/
│   │   │   ├── WelcomeCard.tsx
│   │   │   ├── QuickActionsCard.tsx
│   │   │   ├── ActivityFeed.tsx
│   │   │   ├── SystemHealthCard.tsx
│   │   │   ├── RevenueOverviewCard.tsx
│   │   │   ├── RevenueChart.tsx             # recharts AreaChart
│   │   │   ├── OrderMetricsCard.tsx
│   │   │   ├── OrderStatusChart.tsx         # recharts BarChart
│   │   │   ├── CustomerMetricsCard.tsx
│   │   │   ├── ProductPerformanceCard.tsx
│   │   │   ├── BlogStatsCard.tsx
│   │   │   ├── PublishingTrendChart.tsx
│   │   │   ├── LowStockAlertsCard.tsx
│   │   │   ├── RecentReceiptsCard.tsx
│   │   │   └── InventoryValueCard.tsx
│   │   └── DashboardSkeleton.tsx            # Loading skeleton
│   └── hooks/
│       └── (extend existing useDashboard.ts)
```

#### DashboardPage Layout

```tsx
// DashboardPage.tsx (conceptual)
export const DashboardPage = () => {
  const { t } = useTranslation('common')
  const { user } = useAuthContext()
  const { data: features } = useFeatures()
  usePageContext('Dashboard')

  const isEcommerceEnabled = features?.['Ecommerce.Orders']?.isEffective ?? false
  const isBlogEnabled = features?.['Content.Blog']?.isEffective ?? false
  const isInventoryEnabled = features?.['Ecommerce.Inventory']?.isEffective ?? false

  return (
    <div className="py-6 space-y-6">
      <PageHeader icon={LayoutDashboard} title={t('dashboard.title')}
        description={t('dashboard.welcome', { name: user?.fullName })} responsive />

      {/* CSS Grid: auto-fill with minmax for responsive */}
      <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6">
        <Suspense fallback={<DashboardSkeleton />}>
          <CoreWidgetGroup />                              {/* Always rendered */}
        </Suspense>

        {isEcommerceEnabled && (
          <Suspense fallback={<DashboardSkeleton />}>
            <EcommerceWidgetGroup />                        {/* Lazy loaded */}
          </Suspense>
        )}

        {isBlogEnabled && (
          <Suspense fallback={<DashboardSkeleton />}>
            <BlogWidgetGroup />
          </Suspense>
        )}

        {isInventoryEnabled && (
          <Suspense fallback={<DashboardSkeleton />}>
            <InventoryWidgetGroup />
          </Suspense>
        )}
      </div>
    </div>
  )
}
```

#### Hooks (extend `useDashboard.ts`)

```tsx
// Add to existing src/hooks/useDashboard.ts
export const dashboardKeys = {
  all: ['dashboard'] as const,
  metrics: (params?) => [...dashboardKeys.all, 'metrics', params] as const,     // existing
  core: () => [...dashboardKeys.all, 'core'] as const,                          // new
  ecommerce: (params?) => [...dashboardKeys.all, 'ecommerce', params] as const, // new
  blog: (params?) => [...dashboardKeys.all, 'blog', params] as const,           // new
  inventory: (params?) => [...dashboardKeys.all, 'inventory', params] as const,  // new
}

export const useCoreDashboard = () => useQuery({
  queryKey: dashboardKeys.core(),
  queryFn: () => getCoreDashboard(),
  staleTime: 60_000,
  refetchInterval: 2 * 60_000,  // Core refreshes more frequently (activity feed)
})

// Similar for useEcommerceDashboard, useBlogDashboard, useInventoryDashboard
```

#### Recharts Pattern (copy from ReportsPage)

Use existing `hsl(var(--chart-N))` CSS variables and tooltip styling from `ReportsPage.tsx`:

```tsx
// Consistent chart theme
const CHART_TOOLTIP_STYLE = {
  borderRadius: '8px',
  border: '1px solid hsl(var(--border))',
  backgroundColor: 'hsl(var(--card))',
  color: 'hsl(var(--card-foreground))',
}
```

#### Localization (extend existing 14 keys)

Add ~40 new keys per language covering widget titles, metric labels, chart labels, empty states.

```json
{
  "dashboard": {
    "quickActions": "Quick Actions",
    "pendingOrders": "Pending Orders",
    "pendingReviews": "Pending Reviews",
    "lowStockAlerts": "Low Stock Alerts",
    "draftProducts": "Draft Products",
    "activityFeed": "Recent Activity",
    "systemHealth": "System Health",
    "revenueOverview": "Revenue Overview",
    "revenueChart": "Revenue Trend",
    "orderMetrics": "Order Metrics",
    "customerMetrics": "Customer Metrics",
    "productPerformance": "Product Performance",
    "blogStats": "Blog Statistics",
    "publishingTrend": "Publishing Trend",
    "inventoryAlerts": "Inventory Alerts",
    "recentReceipts": "Recent Receipts",
    "inventoryValue": "Inventory Value",
    "stockMovement": "Stock Movement",
    "viewAll": "View All",
    "noActivity": "No recent activity",
    "vsLastMonth": "vs last month",
    "today": "Today",
    "thisMonth": "This Month",
    "allTime": "All Time"
  }
}
```

### 1.4 Test Plan

| Layer | Test | Count |
|-------|------|-------|
| Unit | `GetCoreDashboardQueryHandlerTests` | ~8 |
| Unit | `GetBlogDashboardQueryHandlerTests` | ~6 |
| Unit | `GetInventoryDashboardQueryHandlerTests` | ~6 |
| Integration | `DashboardEndpoints` (4 new endpoints) | ~8 |
| Architecture | Verify new DTOs follow record patterns | included |

### 1.5 Estimated File Changes

| Action | Files | New/Modified |
|--------|-------|-------------|
| New queries + handlers | 8 | New |
| New DTOs | 3 | New |
| Extend endpoints | 1 | Modified |
| New permission | 1 | Modified |
| ModuleNames constants | 1 | Modified |
| Frontend page + widgets | ~18 | New |
| Extend hooks | 1 | Modified |
| New service functions | 1 | Modified |
| Localization (EN + VI) | 2 | Modified |
| Tests | ~6 | New |
| **Total** | **~42** | |

---

## 2. Media Manager

### 2.1 Pre-requisite: Storage Bug Fixes

Three targeted fixes (~20 lines total):

#### Bug 1: `GetPublicUrl()` cloud provider

```
File: src/NOIR.Infrastructure/Services/FileStorageService.cs
Method: GetPublicUrl() (line 121)
```

**Current:** Always returns `/{mediaUrlPrefix}/{path}` (relative).
**Fix:** Check storage provider — if S3/Azure, return direct bucket/CDN URL.

```csharp
public string? GetPublicUrl(string path)
{
    if (string.IsNullOrEmpty(path)) return null;

    if (_settings.CurrentValue.Provider is "Azure" or "S3")
    {
        var baseUrl = _settings.CurrentValue.Provider == "Azure"
            ? $"https://{_settings.CurrentValue.Azure.AccountName}.blob.core.windows.net/{_settings.CurrentValue.Azure.ContainerName}"
            : $"https://{_settings.CurrentValue.S3.BucketName}.s3.{_settings.CurrentValue.S3.Region}.amazonaws.com";
        return $"{baseUrl}/{path}";
    }

    return $"{_mediaUrlPrefix}/{path}";
}
```

#### Bug 2: CDN URL double-prefix

```
File: src/NOIR.Web/Endpoints/MediaEndpoints.cs (line ~123)
```

**Current:** Blindly prepends `{host}` to URLs that may already be absolute.
**Fix:**

```csharp
var absoluteUrl = relativeUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
    ? relativeUrl  // Already absolute (cloud provider)
    : $"{baseUrl}{relativeUrl}";
```

#### Bug 3: CDN uses filename instead of storagePath

```
File: src/NOIR.Infrastructure/Media/ImageProcessorService.cs (line ~349)
```

**Current:** `url = $"{cdnBaseUrl}/{fileName}"` — loses folder structure.
**Fix:**

```csharp
if (!string.IsNullOrEmpty(_settings.CurrentValue.CdnBaseUrl))
{
    url = $"{_settings.CurrentValue.CdnBaseUrl.TrimEnd('/')}/{storagePath}";
}
```

### 2.2 Architecture Overview

```
┌──────────────────────────────────────────────────────────────────┐
│ MediaLibraryPage (/portal/media)                                 │
│                                                                  │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │ Toolbar: Search | Type Filter | Sort | View Toggle | Upload │ │
│  └─────────────────────────────────────────────────────────────┘ │
│                                                                  │
│  Grid View:                          List View:                  │
│  ┌──────┐ ┌──────┐ ┌──────┐        ┌──────────────────────────┐ │
│  │ img  │ │ img  │ │ img  │        │ thumb | name | type | sz │ │
│  │ name │ │ name │ │ name │        │ thumb | name | type | sz │ │
│  └──────┘ └──────┘ └──────┘        └──────────────────────────┘ │
│                                                                  │
│  ┌──────────────────────────────────────┐                        │
│  │ Pagination / Virtual scroll          │                        │
│  └──────────────────────────────────────┘                        │
└──────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────┐
│ MediaPickerDialog (reusable)                                     │
│                                                                  │
│  Same layout as MediaLibraryPage, plus:                          │
│  - "Select" button per item                                      │
│  - Multi-select mode for galleries                               │
│  - "Upload new" tab                                              │
└──────────────────────────────────────────────────────────────────┘
```

### 2.3 Backend CQRS Refactor

```
src/NOIR.Application/Features/Media/
├── Commands/
│   ├── UploadMediaFile/
│   │   ├── UploadMediaFileCommand.cs       # Extract from MediaEndpoints
│   │   ├── UploadMediaFileCommandHandler.cs
│   │   └── UploadMediaFileCommandValidator.cs
│   ├── DeleteMediaFile/
│   │   ├── DeleteMediaFileCommand.cs
│   │   └── DeleteMediaFileCommandHandler.cs
│   ├── RenameMediaFile/
│   │   ├── RenameMediaFileCommand.cs
│   │   ├── RenameMediaFileCommandHandler.cs
│   │   └── RenameMediaFileCommandValidator.cs
│   └── BulkDeleteMediaFiles/
│       ├── BulkDeleteMediaFilesCommand.cs
│       └── BulkDeleteMediaFilesCommandHandler.cs
├── Queries/
│   ├── GetMediaFiles/
│   │   ├── GetMediaFilesQuery.cs           # Paginated, searchable, filterable
│   │   └── GetMediaFilesQueryHandler.cs
│   └── GetMediaFileById/
│       ├── GetMediaFileByIdQuery.cs
│       └── GetMediaFileByIdQueryHandler.cs
├── DTOs/
│   └── MediaDtos.cs
├── Specifications/
│   └── MediaSpecifications.cs
└── EventHandlers/
    ├── MediaFileUploadedHandler.cs         # Wire existing domain event
    └── MediaFileDeletedHandler.cs          # Cleanup storage on soft-delete
```

#### GetMediaFilesQuery

```csharp
public sealed record GetMediaFilesQuery(
    string? Search = null,           // Filename search
    string? FileType = null,         // "image", "document", "video"
    string? Folder = null,           // "blog", "products", "avatars", etc.
    string SortBy = "createdAt",     // "createdAt", "name", "size"
    SortOrder SortOrder = SortOrder.Descending,
    int Page = 1,
    int PageSize = 24) : IQuery<PaginatedResult<MediaFileDto>>;
```

#### MediaFileDto

```csharp
public sealed record MediaFileDto(
    Guid Id,
    string ShortId,
    string Slug,
    string OriginalFileName,
    string Folder,
    string DefaultUrl,
    string? ThumbHash,
    string? DominantColor,
    int? Width,
    int? Height,
    string Format,
    string MimeType,
    long SizeBytes,
    string? AltText,
    DateTimeOffset CreatedAt,
    IReadOnlyList<MediaVariantDto>? Variants);

public sealed record MediaVariantDto(
    string Name,         // "thumb", "medium", "large"
    string Url,
    string Format,
    int Width,
    int Height,
    long SizeBytes);
```

#### New Endpoints

```
File: src/NOIR.Web/Endpoints/MediaEndpoints.cs (extend existing)

GET    /api/media                         # GetMediaFilesQuery (paginated list)
GET    /api/media/{id:guid}               # Existing GetMediaFileByIdQuery
POST   /api/media/upload                  # Existing (refactor to CQRS)
PUT    /api/media/{id:guid}/rename        # RenameMediaFileCommand
DELETE /api/media/{id:guid}               # DeleteMediaFileCommand (soft delete)
POST   /api/media/bulk-delete             # BulkDeleteMediaFilesCommand
```

#### MediaSpecifications

```csharp
public sealed class MediaFilesFilteredSpec : Specification<MediaFile>
{
    public MediaFilesFilteredSpec(string? search, string? fileType, string? folder,
        string sortBy, SortOrder sortOrder, int page, int pageSize)
    {
        Query.AsNoTracking()
             .TagWith("MediaFilesFiltered");

        if (!string.IsNullOrEmpty(search))
            Query.Where(m => m.OriginalFileName.Contains(search) || m.Slug.Contains(search));

        if (!string.IsNullOrEmpty(fileType))
            Query.Where(m => m.MimeType.StartsWith(fileType));  // "image" → "image/*"

        if (!string.IsNullOrEmpty(folder))
            Query.Where(m => m.Folder == folder);

        // Sorting
        switch (sortBy.ToLowerInvariant())
        {
            case "name": Query.OrderBy(m => m.OriginalFileName, sortOrder); break;
            case "size": Query.OrderBy(m => m.SizeBytes, sortOrder); break;
            default: Query.OrderBy(m => m.CreatedAt, sortOrder); break;
        }

        Query.Skip((page - 1) * pageSize).Take(pageSize);
    }
}
```

### 2.4 Frontend Implementation

#### File Structure

```
src/NOIR.Web/frontend/src/
├── portal-app/media/
│   └── features/media-library/
│       ├── MediaLibraryPage.tsx             # Main page
│       ├── components/
│       │   ├── MediaGrid.tsx                # Grid view (cards with ThumbHash)
│       │   ├── MediaList.tsx                # Table view
│       │   ├── MediaUploadZone.tsx          # Drag-drop upload area
│       │   ├── MediaDetailSheet.tsx         # Side sheet with file details
│       │   └── MediaToolbar.tsx             # Search, filters, sort, view toggle
├── components/
│   └── media/
│       └── MediaPickerDialog.tsx            # Reusable picker for product/blog editors
├── hooks/
│   └── useMediaFiles.ts                     # New TanStack Query hook
├── services/
│   └── media.ts                             # Extend existing with list/delete/rename
```

#### useMediaFiles Hook

```tsx
export const mediaKeys = {
  all: ['media'] as const,
  list: (params: MediaFilesParams) => [...mediaKeys.all, 'list', params] as const,
  detail: (id: string) => [...mediaKeys.all, 'detail', id] as const,
}

export const useMediaFiles = (params: MediaFilesParams) => useQuery({
  queryKey: mediaKeys.list(params),
  queryFn: () => getMediaFiles(params),
  staleTime: 30_000,
  placeholderData: keepPreviousData,  // Smooth pagination transitions
})
```

#### MediaPickerDialog Interface

```tsx
interface MediaPickerDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSelect: (file: MediaFileDto) => void        // Single select
  onSelectMultiple?: (files: MediaFileDto[]) => void  // Gallery mode
  folder?: MediaFolder                           // Pre-filter by folder
  accept?: string[]                              // MIME type filter
}
```

### 2.5 Test Plan

| Layer | Test | Count |
|-------|------|-------|
| Unit | `UploadMediaFileCommandHandler` | ~5 |
| Unit | `DeleteMediaFileCommandHandler` | ~4 |
| Unit | `GetMediaFilesQueryHandler` | ~6 |
| Unit | `RenameMediaFileCommandHandler` | ~4 |
| Integration | Media endpoints (list, delete, rename, bulk-delete) | ~8 |
| Bug fixes | Verify CDN URL construction | ~3 |

### 2.6 Estimated File Changes

| Action | Files | New/Modified |
|--------|-------|-------------|
| Bug fixes | 3 | Modified |
| CQRS commands + handlers | 8 | New |
| Queries + handlers | 4 | New |
| DTOs | 1 | New |
| Specifications | 1 | New |
| Event handlers | 2 | New |
| Extend endpoints | 1 | Modified |
| Frontend page + components | ~8 | New |
| MediaPickerDialog | 1 | New |
| Hooks + services | 2 | New/Modified |
| Localization (EN + VI) | 2 | Modified |
| Tests | ~8 | New |
| **Total** | **~41** | |

---

## 3. Global Search

### 3.1 Architecture Overview

```
┌─────────────────────────────────────────┐
│ CommandPalette (Cmd+K)                  │
│                                         │
│  ┌─ Tabs ─────────────────────────────┐ │
│  │ [Pages] [Search] [Recent] [Actions]│ │
│  └────────────────────────────────────┘ │
│                                         │
│  Search Tab (new):                      │
│  ┌────────────────────────────────────┐ │
│  │ 🔍 Search everything...           │ │
│  │                                    │ │
│  │ Products (3 results)               │ │
│  │   ├─ Product A  SKU: ABC-123      │ │
│  │   ├─ Product B  SKU: DEF-456      │ │
│  │   └─ See all products →            │ │
│  │                                    │ │
│  │ Orders (2 results)                 │ │
│  │   ├─ #ORD-001  John Doe           │ │
│  │   └─ See all orders →              │ │
│  │                                    │ │
│  │ Customers (1 result)               │ │
│  │   └─ John Doe  john@example.com   │ │
│  └────────────────────────────────────┘ │
└─────────────────────────────────────────┘
```

### 3.2 Backend

#### GlobalSearchQuery

```
src/NOIR.Application/Features/Search/
├── Queries/
│   └── GlobalSearch/
│       ├── GlobalSearchQuery.cs
│       └── GlobalSearchQueryHandler.cs
├── DTOs/
│   └── SearchDtos.cs
```

```csharp
public sealed record GlobalSearchQuery(
    string Query,
    string[]? Types = null,   // null = all types; ["products","orders","customers","posts","users"]
    int MaxPerType = 5) : IQuery<GlobalSearchResultDto>;

public sealed record GlobalSearchResultDto(
    IReadOnlyList<SearchResultGroupDto> Groups);

public sealed record SearchResultGroupDto(
    string Type,              // "products", "orders", etc.
    int TotalCount,           // Total matches (for "See all" link)
    IReadOnlyList<SearchResultItemDto> Items);

public sealed record SearchResultItemDto(
    string Id,
    string Title,
    string? Subtitle,
    string? ImageUrl,
    string Url);              // Frontend route: "/portal/products/{id}"
```

#### Handler Implementation

```csharp
public sealed class GlobalSearchQueryHandler
{
    // Inject: IFeatureChecker, IRepository<Product>, IRepository<Order>, etc.

    public async Task<Result<GlobalSearchResultDto>> Handle(GlobalSearchQuery query, CancellationToken ct)
    {
        var tasks = new List<Task<SearchResultGroupDto?>>();
        var types = query.Types ?? ["products", "orders", "customers", "posts", "users"];

        if (types.Contains("products") && await _featureChecker.IsEnabledAsync(ModuleNames.Ecommerce.Products))
            tasks.Add(SearchProductsAsync(query.Query, query.MaxPerType, ct));

        if (types.Contains("orders") && await _featureChecker.IsEnabledAsync(ModuleNames.Ecommerce.Orders))
            tasks.Add(SearchOrdersAsync(query.Query, query.MaxPerType, ct));

        // ... similar for customers, posts, users

        var results = await Task.WhenAll(tasks);
        return Result.Success(new GlobalSearchResultDto(results.Where(r => r != null).ToList()!));
    }

    private async Task<SearchResultGroupDto?> SearchProductsAsync(string q, int max, CancellationToken ct)
    {
        // Use existing ProductsSearchSpec with Count query
        var spec = new ProductsSearchSpec(q, max);
        var items = await _productRepo.ListAsync(spec, ct);
        var count = await _productRepo.CountAsync(new ProductsSearchCountSpec(q), ct);

        if (items.Count == 0) return null;

        return new SearchResultGroupDto("products", count, items.Select(p =>
            new SearchResultItemDto(p.Id.ToString(), p.Name, p.Sku, p.PrimaryImageUrl, $"/portal/products/{p.Id}")
        ).ToList());
    }
}
```

#### New Endpoint

```
GET /api/search?q=keyword&types=products,orders,customers&maxPerType=5
```

- Requires authentication
- Permission: any authenticated user (results filtered by feature gates)
- Minimum query length: 2 characters (validator)
- Maximum `maxPerType`: 10

### 3.3 Frontend Implementation

#### Enhanced Command Palette

```
src/NOIR.Web/frontend/src/components/command-palette/
├── CommandPalette.tsx          # Modified: add Search tab
├── CommandContext.tsx           # Existing context
├── tabs/
│   ├── PagesTab.tsx            # Extract existing navigation items
│   ├── SearchTab.tsx           # New: content search
│   ├── RecentTab.tsx           # Extract existing recent pages
│   └── ActionsTab.tsx          # Extract existing quick actions
├── SearchResultGroup.tsx       # Grouped results with "See all" link
└── SearchResultItem.tsx        # Individual result row
```

#### Search Debounce Hook

```tsx
// In SearchTab.tsx
const [searchQuery, setSearchQuery] = useState('')
const debouncedQuery = useDeferredValue(searchQuery)  // React 19 built-in

const { data, isLoading } = useQuery({
  queryKey: ['global-search', debouncedQuery],
  queryFn: () => globalSearch(debouncedQuery),
  enabled: debouncedQuery.length >= 2,
  staleTime: 10_000,
})
```

#### "See All" Navigation

When user clicks "See all products", navigate to `/portal/products?search=keyword` and close palette.

### 3.4 Test Plan

| Layer | Test | Count |
|-------|------|-------|
| Unit | `GlobalSearchQueryHandler` — per entity type | ~8 |
| Unit | Feature gate filtering | ~3 |
| Integration | `/api/search` endpoint | ~4 |

### 3.5 Estimated File Changes

| Action | Files | New/Modified |
|--------|-------|-------------|
| GlobalSearchQuery + Handler + Validator | 3 | New |
| SearchDtos | 1 | New |
| Search specifications (per entity) | 5 | New |
| New endpoint | 1 | New |
| CommandPalette refactor (extract tabs) | 5 | Modified/New |
| SearchTab + result components | 3 | New |
| Service function + types | 1 | Modified |
| Localization (EN + VI) | 2 | Modified |
| Tests | ~4 | New |
| **Total** | **~25** | |

---

## 4. Import/Export UI

### 4.1 Architecture Overview

```
┌──────────────────────────────────────────────────────────────────┐
│ Export Flow                                                       │
│                                                                  │
│  List Page → ExportDialog → Format (CSV/Excel) + Columns         │
│  → Synchronous for <1000 rows                                    │
│  → Background job + SSE progress for ≥1000 rows                  │
│                                                                  │
│  ┌──────────┐    ┌────────────┐    ┌──────────────┐             │
│  │ Frontend │───→│  Backend   │───→│  Hangfire    │             │
│  │ Export   │    │  Endpoint  │    │  Export Job  │             │
│  │ Dialog   │    │            │    │  (large)     │             │
│  └──────────┘    └────────────┘    └──────┬───────┘             │
│       ↑                                    │                     │
│       └────── SSE progress channel ────────┘                     │
└──────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────┐
│ Import Flow (Wizard)                                             │
│                                                                  │
│  Step 1        Step 2          Step 3         Step 4             │
│  ┌─────────┐   ┌────────────┐  ┌──────────┐  ┌──────────┐      │
│  │ Upload  │→  │ Column     │→ │ Preview  │→ │ Execute  │      │
│  │ CSV/    │   │ Mapping    │  │ & Valid. │  │ + SSE    │      │
│  │ Excel   │   │            │  │          │  │ Progress │      │
│  └─────────┘   └────────────┘  └──────────┘  └──────────┘      │
│                                                                  │
│  Step 5                                                          │
│  ┌─────────────────────────────────────────────┐                │
│  │ Summary: 95 success, 5 failed               │                │
│  │ [Download Error Report]                      │                │
│  └─────────────────────────────────────────────┘                │
└──────────────────────────────────────────────────────────────────┘
```

### 4.2 Backend Changes

#### Fix Excel Export Stub

Install `ClosedXML` (MIT license, lightweight):

```xml
<!-- src/NOIR.Infrastructure/NOIR.Infrastructure.csproj -->
<PackageReference Include="ClosedXML" Version="0.104.2" />
```

Create Excel export service:

```
src/NOIR.Application/Common/Interfaces/IExportService.cs
src/NOIR.Infrastructure/Services/ExportService.cs
```

```csharp
public interface IExportService
{
    byte[] GenerateCsv<T>(IEnumerable<T> data, IReadOnlyList<ExportColumnDefinition> columns);
    byte[] GenerateExcel<T>(IEnumerable<T> data, IReadOnlyList<ExportColumnDefinition> columns, string sheetName);
}

public sealed record ExportColumnDefinition(
    string Header,
    string PropertyName,
    Func<object, string>? Formatter = null);
```

#### New Export Commands

```
src/NOIR.Application/Features/Customers/Queries/ExportCustomers/
├── ExportCustomersQuery.cs
└── ExportCustomersQueryHandler.cs

src/NOIR.Application/Features/Orders/Queries/ExportOrders/
├── ExportOrdersQuery.cs
└── ExportOrdersQueryHandler.cs
```

```csharp
public sealed record ExportCustomersQuery(
    ExportFormat Format = ExportFormat.Csv,
    string? Search = null,
    string? GroupId = null,
    IReadOnlyList<string>? Columns = null   // null = all columns
) : IQuery<ExportResultDto>;
```

#### New Import Commands

```
src/NOIR.Application/Features/Customers/Commands/ImportCustomers/
├── ImportCustomersCommand.cs
├── ImportCustomersCommandHandler.cs
└── ImportCustomersCommandValidator.cs
```

```csharp
public sealed record ImportCustomersCommand(
    IReadOnlyList<ImportCustomerDto> Customers
) : IAuditableCommand<BulkImportResultDto>;

public sealed record ImportCustomerDto(
    string FullName,
    string Email,
    string? Phone,
    string? Address,
    string? GroupName);       // Lookup by name
```

Pattern: Follow existing `BulkImportProductsCommandHandler` (437 lines) — pre-load lookup data, validate per row, collect errors.

#### Import History Entity

```
src/NOIR.Domain/Entities/ImportHistory.cs
```

```csharp
public class ImportHistory : TenantEntity
{
    public string EntityType { get; private set; }    // "Products", "Customers"
    public string FileName { get; private set; }
    public int TotalRows { get; private set; }
    public int SuccessCount { get; private set; }
    public int FailedCount { get; private set; }
    public string? ErrorReportUrl { get; private set; }
    public Guid ImportedBy { get; private set; }
    public DateTimeOffset ImportedAt { get; private set; }
}
```

Requires: EF configuration, migration, repository.

### 4.3 Frontend Implementation

#### Shared Components

```
src/NOIR.Web/frontend/src/components/import-export/
├── ExportDialog.tsx              # Format + column selection
├── ImportWizard.tsx              # 5-step wizard
├── ImportWizardSteps/
│   ├── UploadStep.tsx            # File upload (CSV/Excel)
│   ├── ColumnMappingStep.tsx     # Map file columns → entity fields
│   ├── PreviewStep.tsx           # Preview rows + validation errors
│   ├── ExecuteStep.tsx           # Progress bar via SSE
│   └── SummaryStep.tsx           # Success/failure counts
├── ImportHistoryDialog.tsx       # Past imports list
└── useImportExport.ts            # Shared hooks
```

#### ExportDialog Interface

```tsx
interface ExportDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  entityType: 'products' | 'customers' | 'orders' | 'posts' | 'inventory'
  availableColumns: ExportColumn[]
  currentFilters?: Record<string, unknown>  // Export filtered data
  onExport: (format: 'csv' | 'xlsx', columns: string[]) => Promise<void>
}

interface ExportColumn {
  key: string
  label: string
  defaultSelected: boolean
}
```

#### Client-side Excel Parsing

Install SheetJS for client-side Excel preview:

```bash
pnpm add xlsx
```

Use in `UploadStep.tsx` to parse uploaded Excel files into JSON for column mapping.

### 4.4 SSE Progress Integration

```tsx
// useExportProgress.ts
export const useExportProgress = (channelId: string | null) => {
  const [progress, setProgress] = useState(0)
  const [status, setStatus] = useState<'idle' | 'running' | 'complete' | 'error'>('idle')

  useEffect(() => {
    if (!channelId) return
    const source = new EventSource(`/api/sse?channelId=${channelId}`)
    source.addEventListener('export_progress', (e) => {
      const data = JSON.parse(e.data)
      setProgress(data.progress)
    })
    source.addEventListener('export_complete', (e) => {
      setStatus('complete')
      source.close()
    })
    return () => source.close()
  }, [channelId])

  return { progress, status }
}
```

### 4.5 Test Plan

| Layer | Test | Count |
|-------|------|-------|
| Unit | `ExportService` (CSV + Excel generation) | ~6 |
| Unit | `ExportCustomersQueryHandler` | ~4 |
| Unit | `ImportCustomersCommandHandler` | ~8 |
| Integration | Export/Import endpoints | ~6 |
| Integration | Import history | ~3 |

### 4.6 Estimated File Changes

| Action | Files | New/Modified |
|--------|-------|-------------|
| Install ClosedXML + xlsx | 2 | Modified (csproj, package.json) |
| IExportService + impl | 2 | New |
| Fix Excel stub in ReportQueryService | 1 | Modified |
| New export queries (Customers, Orders) | 4 | New |
| New import commands (Customers) | 3 | New |
| ImportHistory entity + config + migration | 4 | New |
| New endpoints | 2 | Modified |
| Frontend shared components | ~10 | New |
| Hooks + services | 3 | New |
| Localization (EN + VI) | 2 | Modified |
| Tests | ~8 | New |
| **Total** | **~41** | |

---

## 5. Bulk Actions Enhancement

### 5.1 Architecture: Shared Infrastructure

```
┌──────────────────────────────────────────────────────────────┐
│ Reusable Bulk Action System                                  │
│                                                              │
│  ┌─────────────────────┐  ┌──────────────────────────────┐  │
│  │ useBulkSelection<T> │  │ <BulkActionToolbar />        │  │
│  │                     │  │                              │  │
│  │ - selectedIds       │  │ - Selected count badge       │  │
│  │ - toggleSelect      │  │ - Configurable action btns   │  │
│  │ - selectAll         │  │ - Clear selection            │  │
│  │ - selectNone        │  │ - Confirmation dialog        │  │
│  │ - isAllSelected     │  │                              │  │
│  │ - selectedCount     │  │                              │  │
│  └─────────────────────┘  └──────────────────────────────┘  │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐    │
│  │ <BulkCheckboxColumn />                               │    │
│  │                                                      │    │
│  │ - Header checkbox (select all on page)               │    │
│  │ - Row checkboxes                                     │    │
│  │ - Row highlight when selected                        │    │
│  └──────────────────────────────────────────────────────┘    │
└──────────────────────────────────────────────────────────────┘
```

### 5.2 Shared Hook

```
src/NOIR.Web/frontend/src/hooks/useBulkSelection.ts
```

```tsx
interface UseBulkSelectionOptions<T> {
  items: T[] | undefined
  getId: (item: T) => string
}

interface UseBulkSelectionReturn {
  selectedIds: Set<string>
  selectedCount: number
  isAllSelected: boolean
  isPartiallySelected: boolean
  toggleSelect: (id: string) => void
  selectAll: () => void
  selectNone: () => void
  toggleSelectAll: () => void
  isSelected: (id: string) => boolean
}

export const useBulkSelection = <T>({ items, getId }: UseBulkSelectionOptions<T>): UseBulkSelectionReturn => {
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set())

  // Reset selection when items change (pagination, filters)
  useEffect(() => { setSelectedIds(new Set()) }, [items])

  const toggleSelect = useCallback((id: string) => {
    setSelectedIds(prev => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })
  }, [])

  const selectAll = useCallback(() => {
    if (!items) return
    setSelectedIds(new Set(items.map(getId)))
  }, [items, getId])

  const selectNone = useCallback(() => setSelectedIds(new Set()), [])

  const isAllSelected = (items?.length ?? 0) > 0 && items!.every(item => selectedIds.has(getId(item)))
  const isPartiallySelected = selectedIds.size > 0 && !isAllSelected

  return {
    selectedIds,
    selectedCount: selectedIds.size,
    isAllSelected,
    isPartiallySelected,
    toggleSelect,
    selectAll,
    selectNone,
    toggleSelectAll: isAllSelected ? selectNone : selectAll,
    isSelected: (id) => selectedIds.has(id),
  }
}
```

### 5.3 Shared Components

```
src/NOIR.Web/frontend/src/components/bulk-actions/
├── BulkActionToolbar.tsx
├── BulkCheckboxCell.tsx
└── BulkConfirmDialog.tsx
```

#### BulkActionToolbar

```tsx
interface BulkAction {
  key: string
  label: string
  icon: LucideIcon
  variant?: ButtonVariant
  onClick: () => void
  disabled?: boolean
  loading?: boolean
  count?: number           // Show filtered count (e.g., "Publish 3 draft")
}

interface BulkActionToolbarProps {
  selectedCount: number
  actions: BulkAction[]
  onClear: () => void
}
```

Renders a sticky bar at top of table with selected count badge, action buttons, and clear button. Matches existing Products/Reviews design: `bg-primary/5 border border-primary/20 rounded-lg`.

#### BulkConfirmDialog

```tsx
interface BulkConfirmDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  title: string
  description: string
  itemCount: number
  variant?: 'default' | 'destructive'
  onConfirm: () => Promise<void>
  confirmLabel?: string
}
```

### 5.4 New Backend Bulk Commands

| Entity | Command | Priority |
|--------|---------|----------|
| Orders | `BulkUpdateOrderStatusCommand(List<Guid> OrderIds, OrderStatus NewStatus)` | High |
| Customers | `BulkAddToGroupCommand(List<Guid> CustomerIds, Guid GroupId)` | High |
| Customers | `BulkRemoveFromGroupCommand(List<Guid> CustomerIds, Guid GroupId)` | High |
| Blog Posts | `BulkPublishPostsCommand(List<Guid> PostIds)` | Medium |
| Blog Posts | `BulkUnpublishPostsCommand(List<Guid> PostIds)` | Medium |
| Blog Posts | `BulkDeletePostsCommand(List<Guid> PostIds)` | Medium |

Each follows the established pattern from Products:
1. Command + Handler + Validator (co-located)
2. Max batch size: 1000 (use `ProductConstants.MaxBulkOperationSize` or extract to shared constant)
3. Batch load with `AsTracking()` spec
4. Per-item validation, collect errors
5. Single `SaveChangesAsync()`
6. Return `BulkOperationResultDto`
7. Implement `IAuditableCommand`

### 5.5 Migration Plan

1. Create shared hook and components
2. Migrate Products page to use `useBulkSelection` + `BulkActionToolbar` (refactor, no behavior change)
3. Migrate Reviews page similarly
4. Add bulk actions to Orders, Customers, Blog Posts pages
5. Add new backend commands as needed

### 5.6 Test Plan

| Layer | Test | Count |
|-------|------|-------|
| Unit | New bulk command handlers (6 commands) | ~24 |
| Unit | Validators | ~6 |
| Integration | New bulk endpoints | ~6 |

### 5.7 Estimated File Changes

| Action | Files | New/Modified |
|--------|-------|-------------|
| `useBulkSelection` hook | 1 | New |
| `BulkActionToolbar` + `BulkConfirmDialog` | 3 | New |
| Extract shared constant for max batch | 1 | Modified |
| New backend commands (6 x 3 files) | 18 | New |
| New endpoints | 3 | Modified |
| Migrate Products page | 1 | Modified |
| Migrate Reviews page | 1 | Modified |
| Add bulk to Orders page | 1 | Modified |
| Add bulk to Customers page | 1 | Modified |
| Add bulk to Blog Posts page | 1 | Modified |
| Localization (EN + VI) | 2 | Modified |
| Tests | ~12 | New |
| **Total** | **~45** | |

---

## 6. Implementation Order & Dependencies

```
Phase 0: Media Bug Fixes                    [3 files, ~20 LoC]
    │
    ├── No dependencies. Fix immediately.
    │
Phase 1: Dashboard                           [~42 files]
    │
    ├── Depends on: Feature Management (exists), recharts (installed)
    ├── Backend: 4 new queries + endpoints
    ├── Frontend: Widget groups + charts
    │
Phase 2: Media Manager                       [~41 files]
    │
    ├── Depends on: Phase 0 bug fixes
    ├── Backend: CQRS refactor + list/delete/rename
    ├── Frontend: MediaLibraryPage + MediaPickerDialog
    │
Phase 3: Global Search                       [~25 files]
    │
    ├── Depends on: Existing search specs (all 19 queries)
    ├── Backend: GlobalSearchQuery + endpoint
    ├── Frontend: Enhanced CommandPalette
    │
Phase 4: Import/Export                       [~41 files]
    │
    ├── Depends on: ClosedXML install, xlsx install
    ├── Backend: IExportService + import/export commands
    ├── Frontend: ExportDialog + ImportWizard
    │
Phase 5: Bulk Actions                        [~45 files]
    │
    ├── Depends on: Existing bulk patterns
    ├── Backend: 6 new bulk commands
    ├── Frontend: Shared hook + components + migration
```

### Total Scope Summary

| Feature | Backend Files | Frontend Files | Test Files | Total |
|---------|--------------|---------------|-----------|-------|
| Media Bug Fixes | 3 | 0 | 3 | 6 |
| Dashboard | 13 | 20 | 6 | 39 |
| Media Manager | 17 | 13 | 8 | 38 |
| Global Search | 10 | 10 | 4 | 24 |
| Import/Export | 14 | 13 | 8 | 35 |
| Bulk Actions | 22 | 10 | 12 | 44 |
| **Grand Total** | **79** | **66** | **41** | **186** |

### Risk Assessment

| Feature | Risk | Mitigation |
|---------|------|-----------|
| Dashboard | Low | Backend exists, frontend is UI work |
| Media Manager | Medium | CQRS refactor of existing inline code |
| Global Search | Low | Leverages existing search specs |
| Import/Export | Medium | New library (ClosedXML), SSE integration |
| Bulk Actions | Low | Pattern well-established in Products/Reviews |

---

> **Next step:** After design approval, use `/sc:implement` per phase.
