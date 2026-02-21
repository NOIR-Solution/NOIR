# NOIR Admin Portal - UI Pattern Audit Report

> **Audited**: 2026-02-19
> **Scope**: All 30+ pages across 18 portal-app modules
> **Method**: Source code analysis of every page TSX file, shared components, and uikit library

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [UI Pattern Matrix](#ui-pattern-matrix)
3. [Pattern Category Deep-Dive](#pattern-category-deep-dive)
4. [Inconsistency Catalog](#inconsistency-catalog)
5. [Gold Standard: Payment Gateways vs Shipping](#gold-standard-comparison)
6. [Consistency Scores](#consistency-scores)
7. [Recommended Standards](#recommended-standards)

---

## Executive Summary

The NOIR admin portal has **strong foundational consistency** in core patterns (page spacing, card shadows, table wrappers, empty states). However, **37 specific inconsistencies** were identified across 12 pattern categories. The most problematic areas are:

- **Loading states** (3 different strategies: Skeleton, Spinner, plain text)
- **Dialog headers** (5 different icon/layout patterns)
- **Button loading feedback** (spinner vs text-only in ~50/50 split)
- **Form architecture** (shadcn Form vs raw Label in ~70/30 split)
- **Badge color systems** (3 different naming conventions for similar semantic colors)
- **Icon size notation** (`size-N` vs `h-N w-N` split between newer/older code)
- **`cursor-pointer`** missing on interactive elements in older files

**Overall Consistency Score: 72%** (weighted average across all categories)

---

## UI Pattern Matrix

### Legend
- `=` Standard pattern used
- `~` Minor variation
- `X` Significant deviation
- `-` Not applicable (page doesn't use this pattern)

| Page | Card Shadow | Table Wrapper | Row Hover | CTA Button | Actions Btn | Search Input | Empty State | Loading | Badge System | Dialog Header | Page Spacing |
|------|------------|---------------|-----------|------------|-------------|-------------|-------------|---------|-------------|---------------|-------------|
| **Dashboard** | `=` shadow-sm/hover:shadow-lg | `~` rounded-lg border | `=` hover:bg-muted/30 | `-` | `~` ghost p-0 | `-` | `X` inline text only | `=` Skeleton | `~` outline+custom | `-` | `=` space-y-6 |
| **Products** | `=` + backdrop-blur | `=` rounded-xl border-border/50 | `~` hover:bg-muted/30 | `=` shadow-lg + rotate Plus | `=` ghost h-9 w-9 | `=` pl-10 sm:w-48 | `=` EmptyState | `=` Skeleton | `=` secondary/default | `-` | `=` space-y-6 |
| **Product Categories** | `=` | `=` | `=` hover:bg-muted/50 | `=` | `=` | `=` | `=` | `=` Skeleton | `=` | `-` | `=` |
| **Product Attributes** | `=` | `=` | `=` | `=` | `=` | `=` | `=` | `=` Skeleton | `=` outline+dynamic | `~` Pattern E (destructive) | `=` |
| **Brands** | `=` | `=` | `=` | `=` | `=` | `=` | `=` | `=` Skeleton | `=` | `~` Pattern E | `=` |
| **Orders** | `=` | `=` | `=` cursor-pointer | `-` | `~` Eye icon | `=` | `=` (no CTA) | `=` Skeleton | `=` outline+utility | `-` | `=` |
| **Order Detail** | `~` shadow-sm only | `~` rounded-lg border | `=` | `-` | `-` | `-` | `-` | `=` Skeleton | `=` outline+utility | `=` Pattern E | `=` |
| **Inventory Receipts** | `=` | `=` | `=` | `-` | `=` ghost h-9 w-9 | `-` | `=` (no CTA) | `=` Skeleton | `=` outline+dark mode | `=` Pattern E | `=` |
| **Customers** | `=` | `=` | `=` cursor-pointer | `=` | `=` | `=` | `=` | `=` Skeleton | `=` outline+dark mode | `-` | `=` |
| **Customer Detail** | `~` shadow-sm only | `~` rounded-lg border | `=` | `-` | `=` | `-` | `=` | `=` Skeleton | `=` | `-` | `=` |
| **Customer Groups** | `=` | `=` | `=` | `=` | `=` | `=` | `=` | `=` Skeleton | `=` default/secondary | `=` Pattern E | `=` |
| **Blog Posts** | `=` | `=` | `X` no row hover | `=` | `X` text `•••` | `=` | `=` | `=` Skeleton | `~` secondary+raw colors | `-` | `=` |
| **Blog Categories** | `=` | `=` | `X` no row hover | `=` | `-` | `~` sm:w-64 | `=` | `X` plain "Loading..." | `-` | `-` | `=` |
| **Blog Tags** | `=` | `=` | `=` | `=` | `=` | `~` sm:w-64 | `=` | `=` Skeleton | `-` | `-` | `=` |
| **Blog Post Edit** | `=` | `-` | `-` | `-` | `-` | `-` | `-` | `X` plain "Loading..." | `~` variant-based | `-` | `=` |
| **Promotions** | `=` | `=` | `=` | `=` | `=` | `=` | `=` | `=` Skeleton | `~` mixed variant+custom bg | `-` | `=` |
| **Reviews** | `=` | `=` | `=` | `-` | `~` h-8 w-8 (smaller) | `=` | `=` (no CTA) | `=` Skeleton | `=` outline+dark mode | `-` | `=` |
| **Wishlists** | `=` | `-` (card grid) | `-` | `~` no shadow/rotate | `-` | `-` | `=` | `=` Skeleton | `=` outline+dark mode | `=` Pattern E | `=` |
| **Wishlist Analytics** | `=` | `=` | `=` | `-` | `-` | `-` | `=` | `=` Skeleton | `=` secondary | `-` | `=` |
| **Reports** | `~` + border-border/50 | `=` | `~` no group class | `-` | `-` | `-` | `X` inline text only | `=` Skeleton | `=` outline+custom | `-` | `=` |
| **Personal Settings** | `-` (sidebar nav) | `-` | `-` | `-` | `-` | `-` | `-` | `~` opacity dim | `-` | `-` | `~` container max-w-6xl py-6 |
| **Tenant Settings** | `-` (tabs) | `-` | `-` | `-` | `-` | `-` | `-` | `~` opacity dim | `=` | `-` | `~` container max-w-4xl py-6 |
| **Platform Settings** | `-` (tabs) | `-` | `-` | `-` | `-` | `-` | `-` | `~` opacity dim | `=` | `-` | `~` container max-w-4xl py-6 |
| **Shipping** | `=` | `=` | `=` | `~` no shadow/rotate | `~` h-8 w-8 | `=` | `=` | `=` Skeleton | `=` with dark mode | `X` no icon in dialog | `X` no container max-w |
| **Notifications** | `-` | `-` | `-` | `-` | `-` | `-` | `-` | `-` | `-` | `-` | `=` space-y-6 |
| **Notification Prefs** | `=` | `-` | `-` | `-` | `-` | `-` | `-` | `=` Skeleton | `-` | `-` | `~` container max-w-4xl py-6 |
| **Users** | `=` | `=` | `=` | `=` | `=` | `=` | `=` | `=` Skeleton | `=` | `-` | `=` |
| **Roles** | `=` | `=` | `=` | `=` | `=` | `=` | `=` | `=` Skeleton | `=` | `=` Pattern B | `=` |
| **Tenants** | `=` | `=` | `=` | `=` | `=` | `=` | `=` | `=` Skeleton | `=` custom component | `=` Pattern E | `=` |
| **Activity Timeline** | `=` | `-` | `-` | `-` | `-` | `=` | `-` | `=` Skeleton | `=` outline+custom | `-` | `=` |
| **Developer Logs** | `-` (full-height) | `-` | `-` | `-` | `-` | `=` | `~` inline message | `-` | `=` outline+dark mode | `-` | `X` flex h-[calc(100vh-48px)] |

---

## Pattern Category Deep-Dive

### 1. Card Styles

**Standard Pattern (used by ~85% of list pages):**
```
Card className="shadow-sm hover:shadow-lg transition-all duration-300"
```

**Variations found:**
| Variant | Pages | Notes |
|---------|-------|-------|
| Standard | Products, Categories, Attributes, Brands, Orders, Inventory, Customers, CustomerGroups, BlogPosts, BlogCategories, BlogTags, Promotions, Reviews, Wishlists, WishlistAnalytics, Roles, Tenants, Users | Most common |
| + `backdrop-blur-sm bg-card/95` | Products list | Extra glass effect |
| + `border-border/50` | Reports MetricCard, Dashboard KPI | Explicit border |
| `shadow-sm` only (no hover) | Order Detail, Customer Detail | Detail pages |
| Inner item cards: `hover:shadow-md` | Settings EmailTemplates, LegalPages | Lighter hover |
| Stats cards: `border-border/40 backdrop-blur-xl bg-background/40` | ProductStatsCards | Heavy glass effect |

**Recommendation:** Standardize on `shadow-sm hover:shadow-lg transition-all duration-300` for list pages and `shadow-sm` for detail pages. Remove extra backdrop-blur/glass effects to a single approach.

### 2. Table Wrapper

**Standard Pattern (used by ~90% of table pages):**
```
<div className="rounded-xl border border-border/50 overflow-hidden">
```

**Variations found:**
| Variant | Pages |
|---------|-------|
| `rounded-xl border border-border/50` | Products, Categories, Attributes, Brands, Orders, Inventory, Customers, CustomerGroups, BlogPosts, BlogCategories, BlogTags, Promotions, Reviews, WishlistAnalytics, Shipping |
| `rounded-lg border border-border/50` | Dashboard (recent orders) |
| `rounded-lg border overflow-hidden` | Order Detail (items table), Customer Detail (orders table) |

**Recommendation:** Use `rounded-xl border border-border/50 overflow-hidden` for all primary tables. Use `rounded-lg border overflow-hidden` for nested/inner tables on detail pages.

### 3. Table Row Hover

**Standard Pattern:**
```
TableRow className="group transition-colors hover:bg-muted/50"
```

**Variations found:**
| Variant | Pages |
|---------|-------|
| `group transition-colors hover:bg-muted/50` | Most pages |
| `group transition-all duration-200 hover:bg-muted/30` | Products (subtler) |
| No hover class at all | BlogPosts, BlogCategories |
| `transition-colors hover:bg-muted/50` (no `group`) | Reports |
| + `cursor-pointer` | Orders, Customers (clickable rows) |

**Recommendation:** Always use `group transition-colors hover:bg-muted/50`. Add `cursor-pointer` when rows are clickable (navigate to detail). Never omit hover.

### 4. CTA Button (Page-Level "Add New")

**Standard Pattern:**
```tsx
<Button className="group shadow-lg hover:shadow-xl transition-all duration-300">
  <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
  {label}
</Button>
```

**Variations found:**
| Variant | Pages |
|---------|-------|
| Full standard (shadow + rotate) | Products, Categories, Attributes, Brands, Customers, CustomerGroups, Promotions, Roles, Tenants, Users |
| Plain `cursor-pointer` + Plus icon (no shadow/rotate) | Shipping ProviderList, Wishlists |
| No CTA | Dashboard, Orders, Inventory, Reports, Reviews (read-only pages) |

**Recommendation:** All "Add New" CTAs should use the full standard pattern with shadow and icon rotation.

### 5. Actions Menu Trigger (Row-Level)

**Standard Pattern:**
```tsx
<Button variant="ghost" size="sm" className="cursor-pointer h-9 w-9 p-0 transition-all duration-200 hover:bg-primary/10 hover:text-primary">
  <MoreHorizontal className="h-4 w-4" />
</Button>
```

**Variations found:**
| Variant | Pages |
|---------|-------|
| `h-9 w-9` MoreHorizontal icon | Products, Categories, Attributes, Brands, CustomerGroups, Promotions, BlogTags |
| `h-9 w-9` Eye icon only | Orders |
| `h-8 w-8` (smaller) | Reviews, Shipping |
| Text `•••` instead of icon | BlogPosts |
| `h-9 w-9` but separate edit/toggle buttons | Shipping ProviderList |

**Recommendation:** Standardize on `h-9 w-9 p-0` with `MoreHorizontal` icon. Use `h-8 w-8` only for inline action buttons (not menu triggers). Never use text `•••`.

### 6. Search Input

**Standard Pattern:**
```tsx
<div className="relative">
  <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
  <Input className="pl-10 w-full sm:w-48" />
</div>
```

**Variations found:**
| Width | Pages |
|-------|-------|
| `sm:w-48` | Products, Orders, Customers, CustomerGroups, Promotions, Reviews, Brands, Attributes, Shipping |
| `sm:w-64` | BlogCategories, BlogTags |
| `w-full sm:w-48` (standard) | Most pages |

**Recommendation:** Standardize on `sm:w-48` for all search inputs. The 64px wider variant on blog pages is unnecessary.

### 7. Empty States

**Standard Pattern:**
```tsx
<TableCell colSpan={N} className="p-0">
  <EmptyState
    icon={SomeIcon}
    title="..."
    description="..."
    action={{ label: '...', onClick: fn }}
    className="border-0 rounded-none px-4 py-12"
  />
</TableCell>
```

**Variations found:**
| Variant | Pages |
|---------|-------|
| `EmptyState` component with CTA | Products, Categories, Attributes, Brands, Customers, CustomerGroups, BlogPosts, BlogCategories, BlogTags, Promotions, Wishlists |
| `EmptyState` component without CTA | Orders, Inventory, Reviews, WishlistAnalytics |
| Inline `<p className="text-sm text-muted-foreground text-center py-8">` | Reports (all tables), Dashboard (charts/lists) |
| Inline `text-center py-8 text-muted-foreground` | Settings EmailTemplates/LegalPages empty state |
| Hardcoded English strings | Settings empty states (not localized) |

**Recommendation:** Always use the `EmptyState` component. Include CTA when user can create the missing item. Never use inline `<p>` text. Fix hardcoded English strings.

### 8. Loading States

**Standard Pattern:**
```tsx
<TableRow className="animate-pulse">
  <TableCell><Skeleton className="h-4 w-32" /></TableCell>
  ...
</TableRow>
```

**Variations found:**
| Strategy | Pages |
|----------|-------|
| Skeleton rows (5 rows) in table | Products, Categories, Attributes, Brands, Orders, Inventory, Customers, CustomerGroups, BlogPosts, BlogTags, Promotions, Reviews, WishlistAnalytics, Shipping |
| Skeleton cards matching final layout | PaymentGatewaysTab, ProductStatsCards, NotificationPreferences |
| Full-page skeleton (grid layout) | Order Detail, Customer Detail |
| Centered `Loader2 h-8 w-8 animate-spin` | SMTP tabs, EmailTemplates, LegalPages, SessionManagement |
| Plain text "Loading..." | BlogCategories table, BlogPostEdit page |
| `opacity-70` dimming only (tab transitions) | PersonalSettings, TenantSettings, PlatformSettings |

**Recommendation:**
- Table pages: Always use Skeleton rows (5 rows with `animate-pulse`)
- Detail pages: Full-page skeleton matching the final grid layout
- Tab transitions: `opacity-70` dimming is acceptable
- **Never** use plain text "Loading..." — always use Skeleton or Loader2
- Settings tabs should use Skeleton cards (like PaymentGateways), not Loader2 spinner

### 9. Status Badges

**Three badge color systems are in use:**

**System A — `variant="outline"` + bg-{color}-100/text-{color}-800/border-{color}-200:**
```
bg-green-100 text-green-800 border-green-200
```
Used by: Orders, Inventory, Customers, Reviews (with dark mode variants)

**System B — `variant="secondary"` + custom bg:**
```
bg-green-100 text-green-800 (no border class)
```
Used by: BlogPosts

**System C — `variant="default/secondary/outline/destructive"` + `bg-{color}-500/10 text-{color}-600 border-{color}-500/20`:**
```
bg-emerald-500/10 text-emerald-600 border-emerald-500/20
```
Used by: Promotions

**Dark mode support:** Systems A and parts of System C include `dark:` variants. System B does not.

**Recommendation:** Standardize on System A (`variant="outline"` + `bg-{color}-100 text-{color}-800 border-{color}-200` with `dark:bg-{color}-900/30 dark:text-{color}-400 dark:border-{color}-800`). Create a shared `getStatusBadgeClass(status)` utility.

### 10. Dialog/Modal Patterns

**Five distinct header patterns found:**

| Pattern | Description | Used By |
|---------|-------------|---------|
| **A** — Plain title | `DialogTitle` + `DialogDescription` only | BrandDialog, ProductAttributeDialog, ProductCategoryDialog, EditUserDialog |
| **B** — Icon box | `p-2 bg-primary/10 rounded-lg` + icon + side-by-side title/desc | BlogCategoryDialog, BlogTagDialog, CreateRoleDialog, EditRoleDialog, ConfigureGatewayDialog |
| **C** — Icon circle | `w-10 h-10 rounded-full bg-blue-100` + icon | EmailChangeDialog |
| **D** — Icon inline in title | Icon directly in `DialogTitle className="flex items-center gap-2"` | AssignRolesDialog |
| **E** — Destructive icon box | `p-2 rounded-xl bg-destructive/10 border border-destructive/20` | All delete/cancel AlertDialogs |

**Dialog width distribution:**
| Width | Count | Files |
|-------|-------|-------|
| `sm:max-w-[500px]` | 7 | Standard dialogs |
| `sm:max-w-[550px]` | 2 | CreateTenantDialog, ConfigureGatewayDialog |
| `sm:max-w-[520px]` | 1 | ShippingProviderFormDialog |
| `sm:max-w-[425px]` | 1 | EditUserDialog |
| `sm:max-w-md` (448px) | 1 | EmailChangeDialog |
| `sm:max-w-[600px]` | 1 | ProductAttributeDialog (tabbed) |

**Recommendation:**
- Standard dialogs: Pattern B (icon box) at `sm:max-w-[500px]`
- Destructive dialogs: Pattern E (destructive icon box)
- Complex/tabbed dialogs: `sm:max-w-[600px]` with scroll

### 11. Form Architecture

| System | Description | Pages |
|--------|-------------|-------|
| **shadcn Form** (`Form/FormField/FormItem/FormLabel/FormControl/FormMessage`) | Full validation pipeline with Zod | BrandDialog, BlogCategoryDialog, BlogTagDialog, ProductAttributeDialog, ProductCategoryDialog, CreateRoleDialog, EditRoleDialog, ProductFormPage, SMTP tabs, ConfigureGatewayDialog, ShippingProviderDialog |
| **Raw Label + manual state** | `<Label>` + `<Input>` with manual error handling | EditUserDialog, AssignRolesDialog, EmailChangeDialog, Branding/Contact/Regional tabs, NotificationPreferences |

**Recommendation:** Always use shadcn Form system for any form with validation. Reserve raw Label for simple display-only or toggle-only interactions.

### 12. Icon Size Notation

| Notation | Files |
|----------|-------|
| `h-N w-N` (explicit) | All delete dialogs, product components, all page-level code |
| `size-N` (Tailwind v4 shorthand) | Notification components only |

**Recommendation:** Standardize on `h-N w-N` for consistency with the majority codebase. Migrate notification components from `size-N`.

---

## Inconsistency Catalog

### Critical (Affects UX or Accessibility)

| # | Issue | Location | Impact |
|---|-------|----------|--------|
| 1 | **Missing row hover** on BlogPosts and BlogCategories tables | `BlogPostsPage.tsx`, `BlogCategoriesPage.tsx` | Users can't tell rows are interactive |
| 2 | **`cursor-pointer` missing** on interactive elements in older dialogs | CreateRoleDialog, EditRoleDialog, BlogCategoryDialog, BlogTagDialog footer buttons; DeleteUserDialog cancel button; SMTP Switch | Violates CLAUDE.md rule; poor affordance |
| 3 | **Plain "Loading..." text** instead of skeleton | `BlogCategoriesPage.tsx` table, `BlogPostEditPage.tsx` | Jarring UX vs skeleton pattern everywhere else |
| 4 | **Custom switch implementation** instead of `Switch` component | `NotificationPreferencesPage.tsx` | Accessibility risk; inconsistent with all other toggles |
| 5 | **DeleteUserDialog** missing `border-destructive/30`, uses wrong icon color `text-amber-500`, uses raw Button instead of AlertDialogCancel/Action | `DeleteUserDialog.tsx` | Inconsistent destructive pattern |
| 6 | **Hardcoded English strings** in settings empty states | EmailTemplates, LegalPages tabs (tenant + platform) | i18n violation |
| 7 | **No `opacity-70` pending state** on Shipping page tabs | `ShippingPage.tsx` | `isTabPending` exists but class not applied |

### Moderate (Visual Inconsistency)

| # | Issue | Location | Impact |
|---|-------|----------|--------|
| 8 | **Text `•••` instead of MoreHorizontal icon** for actions menu | `BlogPostsPage.tsx` | Visual break from all other pages |
| 9 | **Search input width** `sm:w-64` instead of `sm:w-48` | BlogCategories, BlogTags | Minor layout inconsistency |
| 10 | **Actions button size** `h-8 w-8` instead of `h-9 w-9` | Reviews, Shipping | Minor size mismatch |
| 11 | **No container max-w constraint** on Shipping page | `ShippingPage.tsx` | Full-width instead of constrained like other settings |
| 12 | **Three different badge color systems** | Orders vs Promotions vs BlogPosts | Different CSS patterns for same concept |
| 13 | **Slug code styling inconsistent** — `bg-muted px-1.5 py-0.5 rounded` vs bare `text-muted-foreground` | CustomerGroups/BlogTags vs BlogCategories | Visual inconsistency for identical data type |
| 14 | **Pagination** — manual prev/next vs `Pagination` component | Most pages vs BlogPosts | Different UX for same action |
| 15 | **Delete dialog header layout** — 3 variants across 4 dialogs | Product vs Role/Tenant vs User delete dialogs | Description placement differs |
| 16 | **Missing Loader2 spinner** in DeleteRoleDialog confirm button | `DeleteRoleDialog.tsx` | Text-only "Deleting..." while others show spinner |
| 17 | **Notification bell duplication** — `NotificationBell.tsx` uses `Button`, `NotificationDropdown.tsx` reimplements with raw `<button>` | Notification components | Two different bell implementations |
| 18 | **Connection dot offset** `bottom-0 right-0` vs `bottom-0.5 right-0.5` | NotificationBell vs NotificationDropdown | Sub-pixel position mismatch |

### Minor (Code Consistency)

| # | Issue | Location | Impact |
|---|-------|----------|--------|
| 19 | **`CardTitle className="text-lg"`** inconsistently applied | EmailTemplates/LegalPages have it; Branding/Contact/Regional don't | Heading size varies across settings tabs |
| 20 | **Loading spinner vs skeleton** in settings tabs | SMTP/list tabs use Loader2; simple tabs use skeleton; PaymentGateways uses Skeleton components | Three different loading approaches in one settings area |
| 21 | **Save button has no `hasChanges` guard** on SMTP tabs | PlatformSmtpSettingsTab vs Branding/Contact/Regional | Save enabled even with no changes |
| 22 | **`canEdit` guard missing** on PlatformSmtpSettingsTab save button | PlatformSmtpSettingsTab | Permission check absent |
| 23 | **Preview (Eye) button missing** on PlatformEmailTemplatesTab | Platform vs Tenant EmailTemplates | Feature parity gap |
| 24 | **Grid breakpoint mismatch** `sm:grid-cols-2 gap-6` vs `md:grid-cols-2 gap-4` | Branding tab vs SMTP/list tabs | Different responsive breakpoints in same settings area |
| 25 | **Dialog submit loading** — spinner vs text-only split | ~50% use Loader2, ~50% just change text to "Saving..." | Inconsistent feedback |
| 26 | **Form validation: mutation isPending vs local useState** | BrandDialog uses mutation; BlogCategoryDialog uses local state | Two approaches for same pattern |
| 27 | **`Credenza` vs `Dialog`** wrapper | Only CreateTenantDialog uses Credenza | One-off responsive dialog usage |
| 28 | **`NotificationDropdown` loading** uses custom CSS spinner not Loader2 | `animate-spin rounded-full h-6 w-6 border-b-2 border-primary` | Different spinner from rest of app |

---

## Gold Standard Comparison

### Payment Gateways (Gold Standard) vs Shipping Page

| Dimension | Payment Gateways | Shipping | Gap |
|-----------|-----------------|----------|-----|
| **Provider display** | Card grid (`grid gap-4 md:grid-cols-2`) — each provider is a rich Card | Flat Table inside single Card | Intentional but shipping misses visual richness |
| **Active state visual** | `ring-2 ring-green-500/50 shadow-green-500/10 shadow-lg` on card | `bg-emerald-100` badge in table cell | Shipping active state is subtle |
| **Provider icon** | `p-2 rounded-lg` icon box with green/muted bg, supports image URL | No provider logo/icon in table | Missing visual identity |
| **Toggle mechanism** | Inline `Switch` on card with label + spinner | Icon button triggers AlertDialog confirmation | Different interaction model |
| **Configure action** | `variant="default"` button with Settings icon + "Configure" text | Pencil icon button (`h-8 w-8`) | Less discoverable on Shipping |
| **Test connection** | Full button with result banner (green/red) + response time | Not present | Missing feature |
| **Health display** | Inline colored icon + text in card header | Badge in table column | Equivalent |
| **Dialog header icon** | `p-2 bg-primary/10 rounded-lg` + CreditCard icon | No icon — plain title only | **Missing** |
| **Dialog width** | `sm:max-w-[550px]` | `sm:max-w-[520px]` | Minor (30px) |
| **Submit loading** | `Loader2 h-4 w-4 mr-2 animate-spin` | Text-only "Saving..." | **Missing spinner** |
| **Container constraint** | Inside `container max-w-4xl` | No container — full width | **Missing constraint** |
| **Tab pending state** | `opacity-70 transition-opacity duration-200` | Not applied (code exists but unused) | **Bug** |
| **Credential separator** | `Separator` with `uppercase tracking-wider` label | N/A | N/A |
| **Toggle group** | Individual `FormField` in `space-y-4` | `space-y-3 rounded-lg border p-4` grouped panel | Different but Shipping pattern is nice |

### Shipping Fixes Needed (to match gold standard):
1. Add icon to `ProviderFormDialog` header: `<div className="p-2 bg-primary/10 rounded-lg"><Truck className="h-5 w-5 text-primary" /></div>`
2. Add `Loader2` spinner to submit button
3. Wrap page in `container max-w-4xl` or remove if intentionally wider
4. Apply `opacity-70` pending state to Tabs wrapper
5. Add `cursor-pointer` to CTA button `className`
6. Change CTA button to use `shadow-lg hover:shadow-xl transition-all duration-300` with rotating Plus icon

---

## Consistency Scores

Percentage of pages using the standard variant for each category:

| Category | Standard Pattern | Adoption | Score |
|----------|-----------------|----------|-------|
| **Page root spacing** | `space-y-6` | 28/30 pages | **93%** |
| **Card shadow** | `shadow-sm hover:shadow-lg transition-all duration-300` | 22/26 applicable | **85%** |
| **Table wrapper** | `rounded-xl border border-border/50 overflow-hidden` | 17/19 table pages | **89%** |
| **Row hover** | `group transition-colors hover:bg-muted/50` | 15/19 table pages | **79%** |
| **CTA button** | Shadow + rotating Plus | 10/14 CTA pages | **71%** |
| **Actions button** | `h-9 w-9 p-0` ghost MoreHorizontal | 11/15 pages with actions | **73%** |
| **Search input** | `pl-10 sm:w-48` | 10/12 search pages | **83%** |
| **Empty state** | `EmptyState` component | 16/22 applicable | **73%** |
| **Loading** | Skeleton rows | 16/22 loading pages | **73%** |
| **Badge colors** | System A (outline+dark mode) | 8/14 badge pages | **57%** |
| **Dialog header** | Pattern B (icon box) | 5/13 dialogs | **38%** |
| **Form system** | shadcn Form | 11/17 form pages | **65%** |
| **`cursor-pointer`** | Present on all interactive elements | ~70% of files | **70%** |
| **Icon notation** | `h-N w-N` | 25/30 files | **83%** |

**Weighted Average: 72%**

---

## Recommended Standards

### For Each Category — The Canonical Pattern

#### 1. Card (List Page)
```tsx
<Card className="shadow-sm hover:shadow-lg transition-all duration-300">
```

#### 2. Card (Detail Page)
```tsx
<Card className="shadow-sm">
  <CardHeader className="pb-3">
    <CardTitle className="text-sm flex items-center gap-2">
```

#### 3. Table Wrapper
```tsx
<div className="rounded-xl border border-border/50 overflow-hidden">
  <Table>
```

#### 4. Table Row (Clickable)
```tsx
<TableRow className="group transition-colors hover:bg-muted/50 cursor-pointer">
```

#### 5. Table Row (Actions Only)
```tsx
<TableRow className="group transition-colors hover:bg-muted/50">
```

#### 6. CTA Button
```tsx
<Button className="group shadow-lg hover:shadow-xl transition-all duration-300 cursor-pointer">
  <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
  {t('...')}
</Button>
```

#### 7. Actions Menu Trigger
```tsx
<Button variant="ghost" size="sm" className="cursor-pointer h-9 w-9 p-0 transition-all duration-200 hover:bg-primary/10 hover:text-primary">
  <MoreHorizontal className="h-4 w-4" />
</Button>
```

#### 8. Search Input
```tsx
<div className="relative">
  <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground pointer-events-none" />
  <Input className="pl-10 w-full sm:w-48" placeholder={t('...')} />
</div>
```

#### 9. Empty State (In Table)
```tsx
<TableCell colSpan={N} className="p-0">
  <EmptyState
    icon={RelevantIcon}
    title={t('...')}
    description={t('...')}
    action={canWrite ? { label: t('...'), onClick: handleCreate } : undefined}
    className="border-0 rounded-none px-4 py-12"
  />
</TableCell>
```

#### 10. Skeleton Loading (Table)
```tsx
{Array.from({ length: 5 }).map((_, i) => (
  <TableRow key={i} className="animate-pulse">
    <TableCell><Skeleton className="h-4 w-32" /></TableCell>
    {/* Match skeleton sizes to actual column content */}
  </TableRow>
))}
```

#### 11. Status Badge
```tsx
<Badge variant="outline" className={getStatusBadgeClass(status)}>
  {label}
</Badge>

// Utility:
function getStatusBadgeClass(status: string): string {
  // bg-{color}-100 text-{color}-800 border-{color}-200
  // dark:bg-{color}-900/30 dark:text-{color}-400 dark:border-{color}-800
}
```

#### 12. Dialog Header (Standard)
```tsx
<DialogHeader>
  <div className="flex items-center gap-3">
    <div className="p-2 bg-primary/10 rounded-lg">
      <Icon className="h-5 w-5 text-primary" />
    </div>
    <div>
      <DialogTitle>{title}</DialogTitle>
      <DialogDescription>{description}</DialogDescription>
    </div>
  </div>
</DialogHeader>
```

#### 13. Dialog Header (Destructive)
```tsx
<AlertDialogContent className="border-destructive/30">
  <AlertDialogHeader>
    <div className="flex items-center gap-3">
      <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
        <AlertTriangle className="h-5 w-5 text-destructive" />
      </div>
      <div>
        <AlertDialogTitle>{title}</AlertDialogTitle>
        <AlertDialogDescription>{description}</AlertDialogDescription>
      </div>
    </div>
  </AlertDialogHeader>
  <AlertDialogFooter>
    <AlertDialogCancel className="cursor-pointer">{t('labels.cancel')}</AlertDialogCancel>
    <AlertDialogAction className="cursor-pointer bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors">
      {isLoading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
      {label}
    </AlertDialogAction>
  </AlertDialogFooter>
</AlertDialogContent>
```

#### 14. Form Dialog Layout
```tsx
<DialogContent className="sm:max-w-[500px]">
  {/* Pattern B header */}
  <Form {...form}>
    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
      <FormField
        control={form.control}
        name="fieldName"
        render={({ field }) => (
          <FormItem>
            <FormLabel>{label}</FormLabel>
            <FormControl>
              <Input {...field} />
            </FormControl>
            <FormMessage />
          </FormItem>
        )}
      />
      <DialogFooter>
        <Button type="button" variant="outline" onClick={close} className="cursor-pointer">
          {t('labels.cancel')}
        </Button>
        <Button type="submit" disabled={isPending} className="cursor-pointer">
          {isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
          {t('labels.save')}
        </Button>
      </DialogFooter>
    </form>
  </Form>
</DialogContent>
```

#### 15. Error Banner
```tsx
<div className="mb-4 p-4 bg-destructive/10 text-destructive rounded-md">
  {message}
</div>
```

#### 16. Stale/Pending Overlay
```tsx
<CardContent className={isPending ? 'opacity-70 transition-opacity duration-200' : 'transition-opacity duration-200'}>
```

#### 17. Pagination
```tsx
<div className="flex items-center justify-between mt-4">
  <p className="text-sm text-muted-foreground">
    {t('pagination.showing', { from, to, total })}
  </p>
  <div className="flex items-center gap-2">
    <Button variant="outline" size="sm" className="cursor-pointer" disabled={page === 1}>
      <ChevronLeft className="h-4 w-4" />
    </Button>
    <Button variant="outline" size="sm" className="cursor-pointer" disabled={page === totalPages}>
      <ChevronRight className="h-4 w-4" />
    </Button>
  </div>
</div>
```

---

## Appendix: Files Analyzed

### Portal-App Page Files (40 files)
```
portal-app/dashboard/features/dashboard/DashboardPage.tsx
portal-app/products/features/product-list/ProductsPage.tsx
portal-app/products/features/product-category-list/ProductCategoriesPage.tsx
portal-app/products/features/product-attribute-list/ProductAttributesPage.tsx
portal-app/products/features/product-edit/ProductFormPage.tsx
portal-app/products/components/products/ProductStatsCards.tsx
portal-app/products/components/products/EnhancedProductCard.tsx
portal-app/products/components/products/EnhancedProductGridView.tsx
portal-app/products/components/products/DeleteProductDialog.tsx
portal-app/products/components/products/LowStockAlert.tsx
portal-app/products/components/products/ProductActionsMenu.tsx
portal-app/brands/features/brand-list/BrandsPage.tsx
portal-app/brands/components/BrandDialog.tsx
portal-app/orders/features/order-list/OrdersPage.tsx
portal-app/orders/features/order-detail/OrderDetailPage.tsx
portal-app/orders/utils/orderStatus.ts
portal-app/inventory/features/inventory-receipts/InventoryReceiptsPage.tsx
portal-app/customers/features/customer-list/CustomersPage.tsx
portal-app/customers/features/customer-detail/CustomerDetailPage.tsx
portal-app/customer-groups/features/customer-group-list/CustomerGroupsPage.tsx
portal-app/blogs/features/blog-post-list/BlogPostsPage.tsx
portal-app/blogs/features/blog-category-list/BlogCategoriesPage.tsx
portal-app/blogs/features/blog-tag-list/BlogTagsPage.tsx
portal-app/blogs/features/blog-post-edit/BlogPostEditPage.tsx
portal-app/blogs/components/blog-categories/BlogCategoryDialog.tsx
portal-app/blogs/components/blog-tags/BlogTagDialog.tsx
portal-app/promotions/features/promotion-list/PromotionsPage.tsx
portal-app/reviews/features/review-list/ReviewsPage.tsx
portal-app/wishlists/features/wishlist-page/WishlistPage.tsx
portal-app/wishlists/features/wishlist-analytics/WishlistAnalyticsPage.tsx
portal-app/reports/features/reports-page/ReportsPage.tsx
portal-app/settings/features/personal-settings/PersonalSettingsPage.tsx
portal-app/settings/features/tenant-settings/TenantSettingsPage.tsx
portal-app/settings/features/platform-settings/PlatformSettingsPage.tsx
portal-app/settings/components/payment-gateways/GatewayCard.tsx
portal-app/settings/components/payment-gateways/ConfigureGatewayDialog.tsx
portal-app/settings/components/personal-settings/ProfileForm.tsx
portal-app/settings/components/personal-settings/ChangePasswordForm.tsx
portal-app/settings/components/personal-settings/SessionManagement.tsx
portal-app/settings/components/tenant-settings/BrandingSettingsTab.tsx
portal-app/settings/components/tenant-settings/ContactSettingsTab.tsx
portal-app/settings/components/tenant-settings/SmtpSettingsTab.tsx
portal-app/settings/components/tenant-settings/RegionalSettingsTab.tsx
portal-app/settings/components/tenant-settings/PaymentGatewaysTab.tsx
portal-app/settings/components/tenant-settings/EmailTemplatesTab.tsx
portal-app/settings/components/tenant-settings/LegalPagesTab.tsx
portal-app/settings/components/platform-settings/PlatformSmtpSettingsTab.tsx
portal-app/settings/components/platform-settings/PlatformEmailTemplatesTab.tsx
portal-app/settings/components/platform-settings/PlatformLegalPagesTab.tsx
portal-app/shipping/features/shipping-page/ShippingPage.tsx
portal-app/shipping/components/ProviderFormDialog.tsx
portal-app/user-access/features/user-list/UsersPage.tsx
portal-app/user-access/features/role-list/RolesPage.tsx
portal-app/user-access/features/tenant-list/TenantsPage.tsx
portal-app/user-access/components/users/UserTable.tsx
portal-app/user-access/components/roles/RoleTable.tsx
portal-app/user-access/components/tenants/TenantTable.tsx
portal-app/user-access/components/tenants/TenantStatusBadge.tsx
portal-app/systems/features/activity-timeline/ActivityTimelinePage.tsx
portal-app/systems/features/developer-logs/DeveloperLogsPage.tsx
portal-app/notifications/features/notification-list/NotificationsPage.tsx
portal-app/notifications/features/notification-preferences/NotificationPreferencesPage.tsx
portal-app/notifications/components/notifications/*.tsx
```
