# NOIR UI/UX Consistency Audit Report

> Generated: 2026-02-22 | Audited: 44 pages, 46 dialogs, 72 UIKit stories

## Executive Summary

The NOIR portal has strong foundational patterns but significant inconsistencies across pages. The Payment Gateway config page serves as the gold standard. Key findings:

- **32 pages** have at least one pattern deviation from the gold standard
- **19/46 dialogs** (41%) missing the icon+title header pattern
- **100% of list pages** missing `text-lg` on CardTitle
- **Status badge chaos**: 8+ different badge implementations instead of the shared `getStatusBadgeClasses` utility
- **Storybook**: UIKit 100% covered (72/72), but 27 app-level components lack stories (76% overall)
- **Dashboard**: Completely empty placeholder — no content at all

---

## Pattern Inconsistency Matrix

### List Pages (18 audited)

| Pattern | Pass | Fail | Failure Rate |
|---------|------|------|-------------|
| Container `space-y-6` | 18 | 0 | 0% |
| Card `shadow-sm hover:shadow-lg transition-all duration-300` | 18 | 0 | 0% |
| Skeleton loading in same Card | 18 | 0 | 0% |
| Transition classes consistent | 18 | 0 | 0% |
| **CardTitle explicit `text-lg`** | **0** | **18** | **100%** |
| **Error state `rounded-lg`** | **2** | **16** | **89%** |
| **CardHeader flex justify-between** | **4** | **14** | **78%** |
| **Status badges use `getStatusBadgeClasses`** | **10** | **8** | **44%** |
| All buttons `cursor-pointer` | 16 | 2 | 11% |

### Detail/Form Pages (14 audited)

| Pattern | Pass | Fail | Failure Rate |
|---------|------|------|-------------|
| **Container `container max-w-6xl py-6 space-y-6`** | **1** | **13** | **93%** |
| **Card hover shadow + transition** | **4** | **5** | **56%** |
| **CardTitle `text-lg`** | **1** | **10** | **91%** |
| **Error state styled** | **0** | **14** | **100%** |
| **Loading in Card wrapper** | **5** | **5** | **50%** |
| **Button cursor-pointer** | **8** | **5** | **38%** |
| Tabs use `useUrlTab()` | 6 | 1 | 14% |

### Dialogs (46 audited)

| Pattern | Compliant | Deviating | Failure Rate |
|---------|-----------|-----------|-------------|
| Uses Credenza | 46 | 0 | 0% |
| Has explicit `sm:max-w-*` | 36 | 10 | 22% |
| **Icon+title header block** | **27** | **19** | **41%** |
| Footer `cursor-pointer` | 38 | 6 | 13% |
| Form uses `mode:'onBlur'` + zodResolver | 20 | 3 | 13% |
| URL sync (where required) | 22 | 0 | 0% |

---

## Top 10 Issues (Prioritized by Impact)

### CRITICAL — Affects 18+ pages

**1. CardTitle missing `text-lg`** — ALL 32 list+detail pages
Every page omits explicit `text-lg` on `<CardTitle>`. Detail pages use `text-sm` or `text-base`, list pages use default.
Fix: Add `className="text-lg"` to all CardTitle elements in list pages. Define `text-sm` as the accepted variant for detail page info cards.

**2. Status badge chaos** — 8 list pages + detail pages
8+ different implementations: `getStatusBadgeClasses`, `getOrderStatusColor`, `paymentStatusColors`, `PRODUCT_STATUS_CONFIG`, `RECEIPT_TYPE_CONFIG`, `getSegmentBadgeClass`, `getTierBadgeClass`, direct `variant` switching.
Fix: Migrate all to `getStatusBadgeClasses` from `@/utils/statusBadge`. Add missing color mappings to the shared utility.

**3. Error state inconsistent or absent** — 30 pages
List pages use `rounded-md` instead of `rounded-lg`. Detail pages either have no error state or use bare `text-destructive` without the styled container. Some pages rely solely on toasts.
Fix: Standardize to `bg-destructive/10 text-destructive rounded-lg` in a Card wrapper.

### HIGH — Affects 13+ pages

**4. Container classes missing on detail pages** — 13 pages
Detail pages use bare `<div className="space-y-6">` without `container max-w-6xl py-6`.
Fix: Add container classes. Exception: DeveloperLogs (fullscreen viewport is intentional).

**5. CardHeader layout inconsistent** — 14 list pages
Gold standard uses `flex items-center justify-between` with title left and action right. Most pages use plain stacked layout.
Fix: Restructure where a right-side action exists. Read-only pages can keep stacked layout.

**6. Dialog icon+title header missing** — 19 dialogs
Customer, Wishlist, Promotion, and detail-view dialogs use plain text headers instead of the icon block pattern.
Fix: Add `<div className="p-2 bg-primary/10 rounded-lg">` + icon block to form/action dialogs. Read-only detail dialogs can use the simpler icon-in-title variant.

### MEDIUM — Affects 5-10 pages

**7. Card hover shadow missing on detail pages** — 5 pages
PaymentDetail, OrderDetail, ManualCreateOrder, CustomerDetail missing `hover:shadow-lg transition-all duration-300`.
Fix: Add hover shadow to all Card elements.

**8. Loading state outside Card wrapper** — 5 detail pages
PaymentDetail, OrderDetail, ManualCreateOrder, CustomerDetail render Skeleton as raw children, causing layout collapse during load.
Fix: Wrap Skeleton in placeholder Card matching the loaded layout.

**9. Button cursor-pointer missing** — 7 pages + 6 dialogs
ProductFormPage, BlogPostEdit, EmailTemplateEdit, LegalPageEdit header buttons; PermissionsDialog, PromotionFormDialog Cancel buttons.
Fix: Add `cursor-pointer` to all interactive elements.

**10. PersonalSettings uses local state tabs** — 1 page
Uses `useState` + `useTransition` instead of `useUrlTab()`, violating URL-synced tab convention.
Fix: Migrate to `useUrlTab()` for deep-linking support.

---

## Additional Findings

### Dashboard
- Completely empty placeholder — just "Welcome back, Tenant Administrator!" with no content
- Should have KPI cards, recent activity, quick actions

### Storybook Gaps (27 components without stories)
Priority missing stories:
1. `AttributeInputFactory` (4 files use it) — 13 attribute input types
2. `ProductAttributesSection` (3 files)
3. `FilterSidebar` (3 files) — storefront faceted search
4. `StockHistoryTimeline` (2 files)
5. `ImageUploadZone` (2 files)
6. `OrganizationSelection` (2 files) — login tenant selector

### Visual Replica Stories (8 stories)
These UIKit stories recreate components visually but don't import the real component — changes won't break stories:
- Sidebar, CommandPalette, OfflineIndicator, OnboardingChecklist, WelcomeModal, SortableImageGallery, VariantGenerator, BulkVariantEditor

### BrandsPage Image Convention Violation
Brand logo uses raw `<img>` instead of `FilePreviewTrigger` per `image-preview-in-lists.md`.

### CreateUserDialog Architecture Deviation
Only complex form dialog using manual `useState` per field instead of `react-hook-form` + `zodResolver`.

---

## Recommended Fix Phases

### Phase 2A: Quick Wins (batch text replacements, ~30 min per agent)
- P1: Add `text-lg` to all 18 list page CardTitles
- P7: Change `rounded-md` → `rounded-lg` in error states
- P9: Add `cursor-pointer` to all buttons missing it
- P6 partial: Add `cursor-pointer` to 6 dialog Cancel buttons
- Fix 10 delete dialogs missing explicit `sm:max-w-*`

### Phase 2B: Badge Standardization (~1 hour)
- Extend `getStatusBadgeClasses` to cover all needed colors
- Migrate 8 pages from local badge functions to shared utility
- Remove duplicate local badge utilities

### Phase 2C: Layout Consistency (~2 hours)
- Add container classes to 13 detail pages
- Restructure 14 CardHeaders to flex justify-between layout
- Add hover shadow to 5 detail page Cards
- Wrap loading skeletons in Card placeholders on 5 pages

### Phase 2D: Dialog Headers (~1 hour)
- Add icon+title header block to 19 dialogs
- Standardize icon block to `rounded-lg` (not `rounded-xl`)

### Phase 2E: Specific Fixes
- Migrate PersonalSettings to `useUrlTab()`
- Fix BrandsPage to use `FilePreviewTrigger`
- Migrate `CreateUserDialog` to `react-hook-form`
- Add error states to pages that lack them

### Phase 3: Storybook Gaps (~2 hours)
- Write stories for top 6 priority components
- Convert 8 visual replica stories to import real components

---

## Done Criteria Tracking

| Criteria | Current | Target |
|----------|---------|--------|
| Pages following one design language | ~40% | 100% |
| UI Pattern Matrix alignment | Varies | 100% |
| Every component has a story | 76% | 100% |
| `pnpm build-storybook` passes | TBD | 0 errors |
| `pnpm run build` passes | TBD | 0 errors |
| `dotnet build + test` passes | TBD | 0 errors |
| All strings localized | ~95% | 100% |
| Before/after screenshots | Before: captured | After: pending |
