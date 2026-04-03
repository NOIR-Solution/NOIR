# Dashboard, Reports & Welcome — Test Cases

> Pages: /portal/dashboard, /portal/reports, /welcome | Last updated: 2026-04-03 | Git ref: f6f5cd3
> Total: 52 cases | P0: 3 | P1: 25 | P2: 17 | P3: 7

---

## Page: Dashboard (`/portal/dashboard`)

### Happy Path

#### TC-DSH-001: Dashboard loads with Core widgets [P0] [smoke]
- **Pre**: Authenticated user, tenant has data
- **Steps**:
  1. Navigate to `/portal/dashboard`
  2. Verify PageHeader with LayoutDashboard icon, "Dashboard" title, welcome message with user's fullName
  3. Verify Core widget group renders: WelcomeCard, QuickActionsCard, ActivityFeed
- **Expected**: 3-column responsive grid (`md:2, xl:3`). Core widgets always visible. Skeleton shown during loading.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ Welcome message includes user name | ☐ All core widgets render

#### TC-DSH-002: E-commerce widget group renders when enabled [P1] [smoke]
- **Pre**: `Ecommerce.Orders` module enabled for tenant
- **Steps**:
  1. Navigate to dashboard
  2. Verify E-commerce widgets: RevenueOverviewCard, RevenueChart, OrderMetricsCard, OrderStatusChart, CustomerMetricsCard, ProductPerformanceCard
- **Expected**: 6 e-commerce widgets render. Revenue chart shows sales over time. Order metrics show counts by status.
- **Data**: ☐ Revenue excludes Cancelled/Refunded orders | ☐ Order counts match actual data

#### TC-DSH-003: Blog widget group renders when enabled [P1] [regression]
- **Pre**: `Content.Blog` module enabled
- **Steps**:
  1. Navigate to dashboard
  2. Verify Blog widgets: BlogStatsCard, PublishingTrendChart
- **Expected**: Blog stats and publishing trend chart render.

#### TC-DSH-004: Inventory widget group renders when enabled [P1] [regression]
- **Pre**: `Ecommerce.Inventory` module enabled
- **Steps**:
  1. Navigate to dashboard
  2. Verify Inventory widgets: InventoryValueCard, LowStockAlertsCard, RecentReceiptsCard
- **Expected**: 3 inventory widgets render. Low stock alerts show actionable items.

#### TC-DSH-005: CRM widget group renders when enabled [P1] [regression]
- **Pre**: `Erp.Crm` module enabled
- **Steps**:
  1. Navigate to dashboard
  2. Verify CRM widgets: Total Contacts (with companies subtitle), Active Pipeline (with value), Conversion Rate (with won/lost counts)
- **Expected**: 3 CRM metric cards render with correct formatting. Pipeline value uses `formatCurrency`. Conversion rate shows percentage.
- **Data**: ☐ Contacts count matches CRM data | ☐ Pipeline value formatted correctly | ☐ Conversion rate percentage accurate

#### TC-DSH-006: System health card for platform admin only [P1] [security]
- **Pre**: Platform admin user (`isPlatformAdmin` returns true)
- **Steps**:
  1. Login as platform admin
  2. Navigate to dashboard
  3. Verify SystemHealthCard renders
- **Expected**: SystemHealthCard visible only for platform admins. Not shown for regular tenant admins.

#### TC-DSH-007: Quick Actions card labels not truncated [P1] [regression]
- **Pre**: Dashboard loaded, QuickActionsCard visible
- **Steps**:
  1. View Quick Actions card
  2. Verify all action labels are fully visible (not truncated)
  3. Check at 768px and 1024px viewports
- **Expected**: No text truncation on Quick Action buttons/labels. This is a known regression (BUG-002).
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

### Feature Gating

#### TC-DSH-008: Disable E-commerce module hides widgets [P0] [cross-feature]
- **Pre**: E-commerce widgets currently visible
- **Steps**:
  1. Go to Settings > Feature Management
  2. Disable `Ecommerce.Orders` module
  3. Return to Dashboard
- **Expected**: E-commerce widget group (Revenue, Orders, Customers, Products) not rendered. No errors in console. Other widget groups unaffected.

#### TC-DSH-009: Disable Blog module hides widgets [P1] [cross-feature]
- **Pre**: Blog widgets currently visible
- **Steps**:
  1. Disable `Content.Blog` module
  2. Return to Dashboard
- **Expected**: Blog widget group not rendered. Core + other enabled groups still visible.

#### TC-DSH-010: Disable Inventory module hides widgets [P1] [cross-feature]
- **Pre**: Inventory widgets visible
- **Steps**:
  1. Disable `Ecommerce.Inventory` module
  2. Return to Dashboard
- **Expected**: Inventory widget group not rendered.

#### TC-DSH-011: Disable CRM module hides widgets [P1] [cross-feature]
- **Pre**: CRM widgets visible
- **Steps**:
  1. Disable `Erp.Crm` module
  2. Return to Dashboard
- **Expected**: CRM widget group not rendered.

#### TC-DSH-012: All optional modules disabled — only Core widgets [P1] [cross-feature]
- **Pre**: All optional modules disabled
- **Steps**:
  1. Navigate to dashboard
- **Expected**: Only Core widgets visible (WelcomeCard, QuickActionsCard, ActivityFeed). No errors, no empty spaces from missing widget groups.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

### Loading & Error States

#### TC-DSH-013: Loading skeletons during data fetch [P2] [visual]
- **Pre**: Dashboard loading
- **Steps**:
  1. Navigate to dashboard (throttle network if needed)
  2. Observe skeleton placeholders
- **Expected**: `DashboardSkeleton` components render during loading. No flash of empty state. Each widget group has its own Suspense boundary (count: 4 for ecommerce, 2 for blog, 3 for inventory, 3 for CRM).
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-DSH-014: Widget group error state [P2] [edge-case]
- **Pre**: API error for one widget group (e.g., ecommerce API fails)
- **Steps**:
  1. Simulate API error for ecommerce dashboard endpoint
- **Expected**: Failed group shows EmptyState with error message (`dashboard.loadError`). Other widget groups still render normally.

#### TC-DSH-015: New tenant — empty dashboard [P2] [edge-case]
- **Pre**: Brand new tenant, no orders/products/blog posts
- **Steps**:
  1. Navigate to dashboard
- **Expected**: Core widgets render (WelcomeCard still shows, QuickActionsCard with zero counts, ActivityFeed with empty state). Ecommerce widgets show zeroes, not errors.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

### i18n

#### TC-DSH-016: Dashboard in Vietnamese [P1] [i18n]
- **Pre**: Language set to Vietnamese
- **Steps**:
  1. Navigate to dashboard
  2. Check all widget titles, labels, descriptions
  3. Verify CRM widget labels are fully Vietnamese (no "Pipeline" or English mixing)
- **Expected**: All text in Vietnamese. CRM: "Quy trinh ban hang" not "Pipeline hoat dong". Welcome message in Vietnamese.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

### Responsive

#### TC-DSH-017: Dashboard responsive layout [P2] [responsive]
- **Pre**: Dashboard loaded
- **Steps**:
  1. Check at 1440px (3-column grid)
  2. Check at 768px (2-column grid)
  3. Check at 375px (1-column stack)
- **Expected**: Grid adjusts: `xl:3` -> `md:2` -> `1` column. All widgets readable, no overflow.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

### Performance

#### TC-DSH-018: Metrics load via parallel fetch [P2] [performance]
- **Pre**: All modules enabled
- **Steps**:
  1. Open Network tab
  2. Navigate to dashboard
  3. Observe API calls
- **Expected**: Widget group queries fire in parallel (lazy-loaded components via `React.lazy` + `Suspense`). No waterfall of sequential fetches. 7 backend metrics load via `Task.WhenAll` — individual failures don't block others.

#### TC-DSH-019: Lazy-loaded widget groups [P3] [performance]
- **Pre**: Dashboard loading
- **Steps**:
  1. Check Network tab for JS chunk loading
- **Expected**: `EcommerceWidgetGroup`, `BlogWidgetGroup`, `InventoryWidgetGroup`, `CrmWidgetGroup` are separate lazy chunks. Only loaded when their feature flag is enabled.

---

## Page: Reports (`/portal/reports`)

### Happy Path

#### TC-RPT-001: Reports page loads with Revenue tab [P1] [smoke]
- **Pre**: Authenticated user, ecommerce data exists
- **Steps**:
  1. Navigate to `/portal/reports`
  2. Verify 4 tabs: Revenue, Best Sellers, Inventory, Customers
  3. Verify default tab is "revenue" (URL-synced via `useUrlTab`)
  4. Verify date range presets and export button in header
- **Expected**: Revenue tab active. Area chart shows daily revenue for last 30 days. Metric cards show totals with trend indicators.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-RPT-002: Revenue tab — metric cards and chart [P1] [smoke]
- **Pre**: Revenue data for date range
- **Steps**:
  1. View Revenue tab
  2. Check metric cards (revenue total, trend percentage)
  3. Check area chart renders with tooltip on hover
  4. Check category breakdown bar chart
- **Expected**: MetricCards show formatted currency values. TrendingUp/TrendingDown icons with green/red colors. Chart tooltip shows formatted revenue and order count.
- **Data**: ☐ Revenue values match expected totals | ☐ Trend arrows correct direction

#### TC-RPT-003: Best Sellers tab [P1] [regression]
- **Pre**: Products with sales
- **Steps**:
  1. Click "Best Sellers" tab
  2. Verify URL updates to `?tab=bestSellers`
  3. Verify table with product data and images (`FilePreviewTrigger`)
- **Expected**: Product table with image thumbnails (clickable preview), product name, units sold, revenue.

#### TC-RPT-004: Inventory tab [P1] [regression]
- **Pre**: Inventory data exists
- **Steps**:
  1. Click "Inventory" tab
- **Expected**: Inventory report renders with stock levels, low stock alerts.

#### TC-RPT-005: Customers tab [P1] [regression]
- **Pre**: Customer data exists
- **Steps**:
  1. Click "Customers" tab
- **Expected**: Customer acquisition report with metric cards (total, new, returning customers). Uses appropriate icons (Users, UserPlus, UserCheck, Award).

#### TC-RPT-006: Date range filtering [P1] [regression]
- **Pre**: Reports page loaded
- **Steps**:
  1. Use DateRangePresets to select "Last 7 days"
  2. Verify data refreshes for all tabs
  3. Select custom date range
- **Expected**: Queries refetch with new `startDate`/`endDate` ISO strings. Charts and tables update.
- **Data**: ☐ Date range matches selected preset | ☐ Data filtered correctly

#### TC-RPT-007: Export report [P1] [regression]
- **Pre**: Active tab with data
- **Steps**:
  1. Click ExportButton
  2. Select export format
- **Expected**: Export triggered for the active tab's report type (`exportReportType` maps tab to: Revenue, BestSellers, Inventory, CustomerAcquisition).

### Edge Cases

#### TC-RPT-008: Empty revenue data — no chart [P2] [edge-case]
- **Pre**: Date range with no revenue
- **Steps**:
  1. Select a date range with no data
- **Expected**: EmptyState in chart area with BarChart3 icon: "No data available", "Try adjusting the date range to see results."

#### TC-RPT-009: Tab URL sync — direct link to tab [P2] [regression]
- **Pre**: None
- **Steps**:
  1. Navigate to `/portal/reports?tab=inventory`
- **Expected**: Inventory tab active on load.

#### TC-RPT-010: Chart Y-axis formatting [P3] [visual]
- **Pre**: Revenue data with values over 1M
- **Steps**:
  1. View revenue chart
- **Expected**: Y-axis shows formatted values: >=1M shows "1M", >=1K shows "10K". Tooltip shows full formatted currency.

#### TC-RPT-011: Category breakdown chart [P2] [visual]
- **Pre**: Revenue by category data exists
- **Steps**:
  1. View Revenue tab
  2. Scroll to category breakdown
- **Expected**: Horizontal bar chart with top 8 categories. Uses 5 chart color variables rotating. Height adjusts to data count.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-RPT-012: Skeleton loading states [P3] [visual]
- **Pre**: Reports loading (throttle network)
- **Steps**:
  1. Switch tabs
  2. Observe skeleton rows in tables, skeleton in chart areas
- **Expected**: `SkeletonRows` component renders for table content. Chart areas show Skeleton placeholders.

### i18n

#### TC-RPT-013: Reports in Vietnamese [P1] [i18n]
- **Pre**: Language set to Vietnamese
- **Steps**:
  1. Check all tab labels, metric card titles, chart labels, table headers
- **Expected**: All labels translated. Currency formatting respects regional settings.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

### Responsive

#### TC-RPT-014: Reports responsive layout [P2] [responsive]
- **Pre**: Reports page loaded
- **Steps**:
  1. Resize to 768px
  2. Check tab strip wraps or scrolls
  3. Check charts remain readable
- **Expected**: Charts resize via ResponsiveContainer. Metric cards stack. Tabs accessible.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

---

## Page: Welcome/Landing (`/welcome`)

### Happy Path

#### TC-DSH-020: Welcome page renders correctly [P1] [smoke]
- **Pre**: Not authenticated (or any user)
- **Steps**:
  1. Navigate to `/welcome` (or root `/`)
  2. Verify: nav bar with NOIR logo (orbital SVG, `orbital-animated` class), language switcher, theme toggle, portal button
  3. Verify hero: animated badge, gradient headline ("NOIR" in blue-cyan-teal gradient with `text-transparent bg-clip-text`), CTA button
  4. Verify 3 trust indicator cards at bottom
  5. Verify footer with Terms/Privacy links
- **Expected**: Full landing page renders. Animated gradient mesh background. Glassmorphism navigation.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-DSH-021: Portal CTA button navigates correctly [P1] [smoke]
- **Pre**: Welcome page loaded
- **Steps**:
  1. Click "Access Portal" CTA button or nav "Portal" button
- **Expected**: Navigates to `/portal` (which redirects to login if not authenticated, or dashboard if authenticated).

#### TC-DSH-022: Language switcher on welcome page [P2] [i18n]
- **Pre**: Welcome page loaded
- **Steps**:
  1. Use LanguageSwitcher dropdown to switch to Vietnamese
  2. Verify all text updates: badge, headline, description, buttons, trust indicators, footer
- **Expected**: All landing text translates. Badge uses `landing.badge`, headline uses `landing.welcomeTo`, etc.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-DSH-023: Theme toggle on welcome page [P2] [dark-mode]
- **Pre**: Welcome page loaded in light mode
- **Steps**:
  1. Click ThemeToggleCompact
  2. Verify gradient background adapts
  3. Verify glassmorphism nav adapts
  4. Verify trust indicator cards adapt
- **Expected**: Dark mode: background blobs still visible, glass effect maintained, text colors invert properly.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-DSH-024: Welcome page responsive [P2] [responsive]
- **Pre**: Welcome page loaded
- **Steps**:
  1. Check at 1440px, 768px, 375px
  2. Verify headline scales: `text-5xl` (mobile) to `text-8xl` (lg)
  3. Verify trust indicators stack on mobile (1-col) vs 3-col on desktop
  4. Verify breadcrumb "Projects" link hidden on sm (`hidden sm:block`)
- **Expected**: Fully responsive. No horizontal scroll on mobile. Font sizes scale down.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-DSH-025: Footer links work [P3] [regression]
- **Pre**: Welcome page loaded
- **Steps**:
  1. Click "Terms" footer link -> navigates to `/terms`
  2. Click "Privacy" footer link -> navigates to `/privacy`
- **Expected**: Both pages render (TermsPage, PrivacyPage).

### Edge Cases

#### TC-DSH-026: Orbital logo animation [P3] [visual]
- **Pre**: Welcome page loaded
- **Steps**:
  1. Verify NOIR orbital logo SVG has `orbital-animated` CSS class
  2. Verify `aria-hidden="true"` on decorative logos
- **Expected**: Logo circles animate. Both nav logo and footer logo have correct classes.

#### TC-DSH-027: Gradient text rendering [P3] [visual]
- **Pre**: Welcome page in light and dark mode
- **Steps**:
  1. Check "NOIR" text in headline has gradient (blue -> cyan -> teal)
  2. Verify `text-transparent` class is present with `bg-clip-text`
- **Expected**: Gradient text visible in both themes. Without `text-transparent`, text appears solid — this is a known gotcha.

---

## Cross-Feature Tests

#### TC-DSH-028: Dashboard refresh after order creation [P1] [cross-feature]
- **Pre**: Dashboard with ecommerce widgets, create a new order
- **Steps**:
  1. View dashboard, note order count
  2. Create an order in another tab
  3. Return to dashboard, refresh or wait for SignalR update
- **Expected**: Order metrics update. Revenue reflects new order (if not Cancelled/Refunded).

#### TC-DSH-029: Dashboard after module toggle (enable) [P1] [cross-feature]
- **Pre**: CRM module disabled
- **Steps**:
  1. Enable `Erp.Crm` in Feature Management
  2. Navigate to Dashboard
- **Expected**: CRM widget group now renders with 3 metric cards.

#### TC-DSH-030: Reports date uses regional settings [P1] [data-consistency]
- **Pre**: Tenant with DD/MM/YYYY date format
- **Steps**:
  1. Navigate to reports
  2. Check date display in charts and tables
- **Expected**: All dates use `formatDate` / `formatDateTime` from `useRegionalSettings()`. No raw `toLocaleDateString()`.

#### TC-DSH-031: Dashboard concurrent widget loading [P2] [performance]
- **Pre**: All modules enabled
- **Steps**:
  1. Open browser DevTools Performance tab
  2. Navigate to dashboard
  3. Verify no sequential loading waterfall
- **Expected**: Each widget group in its own `Suspense` boundary. Lazy chunks load independently. Backend `Task.WhenAll` ensures no single metric blocks others.

---

## Data Consistency

#### TC-DSH-032: Revenue excludes Cancelled/Refunded [P0] [data-consistency]
- **Pre**: Orders exist including Cancelled and Refunded ones
- **Steps**:
  1. Compare dashboard revenue widget total with Reports revenue total
  2. Verify both exclude Cancelled and Refunded orders
- **Expected**: Revenue figures consistent. Cancelled/Refunded orders excluded from both dashboard and reports.

#### TC-DSH-033: CRM widget uses formatCurrency [P2] [data-consistency]
- **Pre**: CRM pipeline has deals with monetary values
- **Steps**:
  1. View CRM "Active Pipeline" metric card
  2. Verify pipeline value uses `formatCurrency()` utility
- **Expected**: Currency formatted consistently with locale/tenant settings.

---

## Security

#### TC-DSH-034: Dashboard requires authentication [P1] [security]
- **Pre**: Not authenticated
- **Steps**:
  1. Navigate to `/portal/dashboard`
- **Expected**: Redirects to login page. No API calls made without auth token.

#### TC-DSH-035: Reports requires authentication [P1] [security]
- **Pre**: Not authenticated
- **Steps**:
  1. Navigate to `/portal/reports`
- **Expected**: Redirects to login page.

---

## Dark Mode

#### TC-DSH-036: Dashboard dark mode [P2] [dark-mode]
- **Pre**: Dark mode enabled, dashboard loaded
- **Steps**:
  1. Verify all cards have proper dark backgrounds
  2. Verify chart tooltips use `var(--card)` background
  3. Verify CRM icon backgrounds use dark variants (e.g., `dark:bg-blue-900/30`)
  4. Verify trend indicator colors readable
- **Expected**: All widgets readable in dark mode. Chart grid lines use `var(--border)`. No white flashes.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-DSH-037: Reports dark mode charts [P2] [dark-mode]
- **Pre**: Dark mode, reports loaded
- **Steps**:
  1. Check area chart gradient, axes, grid
  2. Check bar chart colors
  3. Check tooltip styling
- **Expected**: Charts use CSS variables for colors. Tooltip has dark background with `var(--card)`.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

---

## Known Regressions

#### TC-DSH-038: BUG-002 Quick Action card labels truncated [P1] [regression]
- **Pre**: Dashboard loaded
- **Steps**:
  1. View QuickActionsCard
  2. Check all quick action button labels at various viewport widths
  3. Pay special attention to longer labels at 768px
- **Expected**: No label truncation. All action text fully visible. If truncated, this is a confirmed regression (BUG-002).
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-DSH-039: Vietnamese CRM widget labels [P1] [i18n] [regression]
- **Pre**: Language set to Vietnamese, CRM module enabled
- **Steps**:
  1. View CRM widgets on dashboard
  2. Check for English words in Vietnamese labels (e.g., "Pipeline")
- **Expected**: All CRM widget text fully Vietnamese. Known issue: "Pipeline hoat dong" should be fully Vietnamese equivalent.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

---

## Widget-Specific Tests

#### TC-DSH-040: Activity Feed widget [P2] [regression]
- **Pre**: Recent activity exists (orders, updates, etc.)
- **Steps**:
  1. View ActivityFeed widget
  2. Verify items show relative timestamps
- **Expected**: Activity items use `formatRelativeTime` (not `formatDateTime` — activity feeds are an allowed exception). Items ordered by recency.

#### TC-DSH-041: Low Stock Alerts widget [P2] [cross-feature]
- **Pre**: Inventory module enabled, products with low stock
- **Steps**:
  1. View LowStockAlertsCard on dashboard
  2. Verify alert items show product name and current stock level
- **Expected**: Alerts render with actionable information. Card is part of embedded settings table (skip DataTable migration, per rules).

#### TC-DSH-042: Revenue chart tooltip formatting [P3] [visual]
- **Pre**: Revenue chart with data
- **Steps**:
  1. Hover over data points in revenue chart
- **Expected**: Tooltip shows: date label (month + day, formatted by timezone), revenue as formatted currency, order count as localeString.

#### TC-DSH-043: Product Performance card [P2] [regression]
- **Pre**: Products with sales data
- **Steps**:
  1. View ProductPerformanceCard in ecommerce widgets
- **Expected**: Top selling products listed. Data comes from `data.topSellingProducts`.

#### TC-DSH-044: Blog Stats card [P2] [regression]
- **Pre**: Blog module enabled, blog posts exist
- **Steps**:
  1. View BlogStatsCard
  2. View PublishingTrendChart
- **Expected**: Stats card shows blog metrics. Trend chart shows publishing frequency over time.

#### TC-DSH-045: Order Status chart [P2] [visual]
- **Pre**: Orders with various statuses
- **Steps**:
  1. View OrderStatusChart on dashboard
- **Expected**: Chart shows distribution of order statuses. Colors match status badge color conventions.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-DSH-046: Inventory Value card [P2] [regression]
- **Pre**: Inventory module enabled, stock exists
- **Steps**:
  1. View InventoryValueCard
- **Expected**: Shows total inventory value summary. Data from `data.valueSummary`.

#### TC-DSH-047: Recent Receipts card [P2] [regression]
- **Pre**: Inventory receipts exist
- **Steps**:
  1. View RecentReceiptsCard
- **Expected**: Shows recent inventory receipts (stock in/out).

#### TC-DSH-048: Revenue Overview card [P1] [regression]
- **Pre**: Ecommerce data exists
- **Steps**:
  1. View RevenueOverviewCard
- **Expected**: Shows total revenue, total orders from `data.revenue`. Formatted with `formatCurrency`.

#### TC-DSH-049: Welcome card personalization [P3] [visual]
- **Pre**: Authenticated user
- **Steps**:
  1. View WelcomeCard widget
- **Expected**: Card displays user's name/greeting. Matches PageHeader welcome message.

#### TC-DSH-050: Customer Metrics card [P2] [regression]
- **Pre**: Ecommerce module enabled
- **Steps**:
  1. View CustomerMetricsCard
- **Expected**: Shows total and new customer counts. Currently hardcoded to 0 in `EcommerceWidgetGroup` (potential data issue to verify).
- **Data**: ☐ Verify if `totalCustomers: 0, newCustomers: 0` is intentional or a bug

#### TC-DSH-051: Reports tab pending transition [P3] [visual]
- **Pre**: Reports page loaded
- **Steps**:
  1. Switch between tabs quickly
  2. Observe opacity transition during `isTabPending`
- **Expected**: Content fades slightly during tab transition (useTransition). No content flash.

#### TC-DSH-052: Reports export type matches active tab [P2] [data-consistency]
- **Pre**: Reports page, Best Sellers tab active
- **Steps**:
  1. Click export while on Best Sellers tab
  2. Verify exported report is "BestSellers" type (not Revenue)
- **Expected**: `exportReportType` correctly maps: revenue->Revenue, bestSellers->BestSellers, inventory->Inventory, customers->CustomerAcquisition.
