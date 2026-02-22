# NOIR Admin Portal - Design Standards

> **Copy-paste ready patterns for all portal-app UI components.**
> Based on: [audit-ui-patterns.md](./audit-ui-patterns.md) | Last updated: 2026-02-19

---

## Quick Index

| # | Pattern | Category |
|---|---------|----------|
| 1 | [Card — List Page](#1-card--list-page) | Layout |
| 2 | [Card — Detail Page](#2-card--detail-page) | Layout |
| 3 | [Table Wrapper](#3-table-wrapper) | Layout |
| 4 | [Table Row — Clickable](#4-table-row--clickable) | Table |
| 5 | [Table Row — Actions Only](#5-table-row--actions-only) | Table |
| 6 | [CTA Button](#6-cta-button-add-new) | Actions |
| 7 | [Actions Menu Trigger](#7-actions-menu-trigger) | Actions |
| 8 | [Search Input](#8-search-input) | Input |
| 9 | [Empty State](#9-empty-state-in-table) | Feedback |
| 10 | [Skeleton Loading](#10-skeleton-loading-table) | Feedback |
| 11 | [Status Badge](#11-status-badge) | Display |
| 12 | [Dialog Header — Standard](#12-dialog-header--standard) | Dialog |
| 13 | [Dialog Header — Destructive](#13-dialog-header--destructive) | Dialog |
| 14 | [Form Dialog Layout](#14-form-dialog-layout) | Dialog |
| 15 | [Error Banner](#15-error-banner) | Feedback |
| 16 | [Stale/Pending Overlay](#16-stalependig-overlay) | Feedback |
| 17 | [Pagination](#17-pagination) | Navigation |

---

## 1. Card — List Page

**Category:** Layout
**Use:** Wrapping table/content sections on list pages (Products, Orders, Customers, etc.)

```tsx
<Card className="shadow-sm hover:shadow-lg transition-all duration-300">
  <CardHeader>
    <CardTitle>{t('...')}</CardTitle>
  </CardHeader>
  <CardContent>
    {/* Table or content */}
  </CardContent>
</Card>
```

**When to use:** Every list page that displays a collection of items.
**When NOT to use:** Detail pages, settings pages with tabs.

**Common mistakes:**
- Adding `backdrop-blur-sm bg-card/95` — unnecessary glass effect
- Adding `border-border/50` explicitly — the shadow is sufficient
- Forgetting `hover:shadow-lg` — makes the card feel dead

---

## 2. Card — Detail Page

**Category:** Layout
**Use:** Section cards on detail pages (Order Detail, Customer Detail).

```tsx
<Card className="shadow-sm">
  <CardHeader className="pb-3">
    <CardTitle className="text-sm flex items-center gap-2">
      <Icon className="h-4 w-4 text-muted-foreground" />
      {t('...')}
    </CardTitle>
  </CardHeader>
  <CardContent>
    {/* Detail content */}
  </CardContent>
</Card>
```

**When to use:** Section cards on entity detail pages.
**When NOT to use:** List pages (use Pattern 1 instead).

**Common mistakes:**
- Adding `hover:shadow-lg` — detail cards don't need hover lift
- Missing `pb-3` on CardHeader — creates too much vertical gap

---

## 3. Table Wrapper

**Category:** Layout
**Use:** Wrapping `<Table>` in all list pages.

```tsx
<div className="rounded-xl border border-border/50 overflow-hidden">
  <Table>
    <TableHeader>
      <TableRow>
        <TableHead>{t('...')}</TableHead>
      </TableRow>
    </TableHeader>
    <TableBody>
      {/* Rows */}
    </TableBody>
  </Table>
</div>
```

**When to use:** All primary tables on list pages.
**When NOT to use:** Nested tables on detail pages — use `rounded-lg border overflow-hidden` instead.

**Common mistakes:**
- Using `rounded-lg` instead of `rounded-xl` for primary tables
- Forgetting `overflow-hidden` — border radius won't clip table corners
- Omitting `border-border/50` — the semi-transparent border is intentional

---

## 4. Table Row — Clickable

**Category:** Table
**Use:** Rows that navigate to a detail page on click (Orders, Customers).

```tsx
<TableRow
  className="group transition-colors hover:bg-muted/50 cursor-pointer"
  onClick={() => navigate(`/portal/path/${item.id}`)}
>
  <TableCell>{/* content */}</TableCell>
</TableRow>
```

**When to use:** Rows where clicking anywhere navigates to a detail view.
**When NOT to use:** Rows that only have action buttons (use Pattern 5).

**Common mistakes:**
- Forgetting `cursor-pointer` — violates CLAUDE.md interactive element rule
- Forgetting `group` — needed for child element hover styling
- Using `hover:bg-muted/30` — standard is `/50`

---

## 5. Table Row — Actions Only

**Category:** Table
**Use:** Rows with action menus but no row-level navigation (Products, Brands, Categories).

```tsx
<TableRow className="group transition-colors hover:bg-muted/50">
  <TableCell>{/* content */}</TableCell>
  <TableCell className="text-right">
    {/* Actions menu trigger (Pattern 7) */}
  </TableCell>
</TableRow>
```

**When to use:** Standard table rows with edit/delete via dropdown menu.
**When NOT to use:** Rows that should navigate on click (use Pattern 4).

**Common mistakes:**
- Omitting `group` — breaks child hover animations
- Omitting hover entirely — rows must always have hover feedback
- Adding `cursor-pointer` — only for clickable rows

---

## 6. CTA Button (Add New)

**Category:** Actions
**Use:** Page-level "Add New" buttons (Add Product, Create Role, etc.)

```tsx
<Button className="group shadow-lg hover:shadow-xl transition-all duration-300 cursor-pointer">
  <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
  {t('...')}
</Button>
```

**When to use:** Primary creation action in page headers.
**When NOT to use:** Read-only pages (Dashboard, Reports, Reviews), inline form submit buttons.

**Common mistakes:**
- Forgetting `shadow-lg hover:shadow-xl` — CTA must have shadow elevation
- Forgetting `group-hover:rotate-90` on Plus icon — signature animation
- Forgetting `cursor-pointer` — required on all interactive elements
- Using a plain `<Plus />` without transition classes

---

## 7. Actions Menu Trigger

**Category:** Actions
**Use:** Row-level "more actions" button that opens a dropdown (edit, delete, etc.)

```tsx
<DropdownMenu>
  <DropdownMenuTrigger asChild>
    <Button
      variant="ghost"
      size="sm"
      className="cursor-pointer h-9 w-9 p-0 transition-all duration-200 hover:bg-primary/10 hover:text-primary"
      aria-label={t('labels.actions')}
    >
      <MoreHorizontal className="h-4 w-4" />
    </Button>
  </DropdownMenuTrigger>
  <DropdownMenuContent align="end">
    <DropdownMenuItem className="cursor-pointer">
      <Pencil className="h-4 w-4 mr-2" />
      {t('labels.edit')}
    </DropdownMenuItem>
    <DropdownMenuItem className="cursor-pointer text-destructive focus:text-destructive">
      <Trash2 className="h-4 w-4 mr-2" />
      {t('labels.delete')}
    </DropdownMenuItem>
  </DropdownMenuContent>
</DropdownMenu>
```

**When to use:** Every table row with edit/delete/toggle actions.
**When NOT to use:** Never. All action rows must use this pattern.

**Common mistakes:**
- Using `h-8 w-8` — standard is `h-9 w-9`
- Using text `•••` instead of `MoreHorizontal` icon
- Forgetting `cursor-pointer` on the trigger button
- Forgetting `aria-label` — required for icon-only buttons
- Missing `cursor-pointer` on `DropdownMenuItem` children

---

## 8. Search Input

**Category:** Input
**Use:** Table search/filter input in page headers.

```tsx
<div className="relative">
  <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground pointer-events-none" />
  <Input
    className="pl-10 w-full sm:w-48"
    placeholder={t('...')}
    value={searchInput}
    onChange={(e) => setSearchInput(e.target.value)}
  />
</div>
```

**When to use:** Any page that supports text search/filtering.
**When NOT to use:** N/A — always use this pattern for search inputs.

**Common mistakes:**
- Using `sm:w-64` — standard is `sm:w-48`
- Forgetting `pointer-events-none` on the Search icon
- Hardcoding placeholder text instead of using `t()`

---

## 9. Empty State (In Table)

**Category:** Feedback
**Use:** When a table has no data to display.

```tsx
import { EmptyState } from '@uikit'

<TableBody>
  {items.length === 0 && !isLoading && (
    <TableRow>
      <TableCell colSpan={COLUMN_COUNT} className="p-0">
        <EmptyState
          icon={RelevantIcon}
          title={t('module.noItemsFound')}
          description={t('module.noItemsDescription')}
          action={canCreate ? {
            label: t('module.addItem'),
            onClick: handleCreate,
          } : undefined}
          className="border-0 rounded-none px-4 py-12"
        />
      </TableCell>
    </TableRow>
  )}
</TableBody>
```

**When to use:** All tables when the data array is empty.
**When NOT to use:** Never skip — don't use inline `<p>` text or hardcoded strings.

**Common mistakes:**
- Using inline `<p className="text-muted-foreground">` instead of `EmptyState`
- Forgetting `className="border-0 rounded-none"` — clashes with table wrapper border
- Forgetting `p-0` on `TableCell` — double padding
- Omitting the `action` prop when user has create permission
- Hardcoding English strings instead of `t()` calls

---

## 10. Skeleton Loading (Table)

**Category:** Feedback
**Use:** Loading state for table data (initial load or refetch).

```tsx
{isLoading && (
  <>
    {Array.from({ length: 5 }).map((_, i) => (
      <TableRow key={i} className="animate-pulse">
        <TableCell><Skeleton className="h-4 w-32" /></TableCell>
        <TableCell><Skeleton className="h-4 w-24" /></TableCell>
        <TableCell><Skeleton className="h-4 w-20" /></TableCell>
        <TableCell><Skeleton className="h-6 w-16 rounded-full" /></TableCell>
        <TableCell className="text-right"><Skeleton className="h-8 w-8 ml-auto" /></TableCell>
      </TableRow>
    ))}
  </>
)}
```

**When to use:** All table pages during data loading.
**When NOT to use:** Tab transitions (use Pattern 16 opacity dimming). Detail page full-page loads (use grid-matching skeleton).

**Common mistakes:**
- Using plain text "Loading..." — never do this
- Using `Loader2` spinner for tables — spinners are only for settings/tab sub-views
- Not matching skeleton widths to actual column content
- Forgetting `animate-pulse` on `TableRow`
- Using fewer than 5 skeleton rows

---

## 11. Status Badge

**Category:** Display
**Use:** Displaying entity status (order status, receipt status, user active/inactive, etc.)

```tsx
import { Badge } from '@uikit'

// Standard usage:
<Badge variant="outline" className={getStatusColor(status)}>
  {t(`statuses.${status}`)}
</Badge>
```

**Status color utility pattern** (`variant="outline"` + System A colors):

```tsx
// ALWAYS use the shared utility — NEVER define local badge functions
import { getStatusBadgeClasses } from '@/utils/statusBadge'

<Badge variant="outline" className={getStatusBadgeClasses('green')}>Active</Badge>
<Badge variant="outline" className={getStatusBadgeClasses('red')}>Cancelled</Badge>
<Badge variant="outline" className={getStatusBadgeClasses('yellow')}>Pending</Badge>
<Badge variant="outline" className={getStatusBadgeClasses('blue')}>Processing</Badge>
<Badge variant="outline" className={getStatusBadgeClasses('gray')}>Draft</Badge>
```

**Color palette reference:**

| Semantic | Light | Dark |
|----------|-------|------|
| Success/Active | `bg-green-100 text-green-800 border-green-200` | `dark:bg-green-900/30 dark:text-green-400 dark:border-green-800` |
| Warning/Pending | `bg-amber-100 text-amber-800 border-amber-200` | `dark:bg-amber-900/30 dark:text-amber-400 dark:border-amber-800` |
| Error/Cancelled | `bg-red-100 text-red-800 border-red-200` | `dark:bg-red-900/30 dark:text-red-400 dark:border-red-800` |
| Info/Processing | `bg-blue-100 text-blue-800 border-blue-200` | `dark:bg-blue-900/30 dark:text-blue-400 dark:border-blue-800` |
| Neutral/Inactive | `bg-gray-100 text-gray-800 border-gray-200` | `dark:bg-gray-900/30 dark:text-gray-400 dark:border-gray-800` |

**When to use:** All status indicators across the app.
**When NOT to use:** N/A — always use this pattern for status display.

**Common mistakes:**
- Using `variant="secondary"` + custom bg without border (System B) — inconsistent
- Using `/10` opacity variants (System C) — not the standard
- Forgetting dark mode variants — always include `dark:` classes
- Mixing `emerald` vs `green` — pick one per semantic meaning and be consistent
- **Creating local badge functions** (`getOrderStatusColor`, `paymentStatusColors`, `getSegmentBadgeClass`, etc.) — use `getStatusBadgeClasses` from `@/utils/statusBadge` instead
- Using `variant={isActive ? 'default' : 'secondary'}` — use outline + `getStatusBadgeClasses`

**Reference:** `@/utils/statusBadge` is the single source of truth for badge colors.

---

## 12. Dialog Header — Standard

**Category:** Dialog
**Use:** All non-destructive dialogs (create, edit, configure).

```tsx
<DialogContent className="sm:max-w-[500px]">
  <DialogHeader>
    <div className="flex items-center gap-3">
      <div className="p-2 bg-primary/10 rounded-lg">
        <Icon className="h-5 w-5 text-primary" />
      </div>
      <div>
        <DialogTitle>{t('...')}</DialogTitle>
        <DialogDescription>{t('...')}</DialogDescription>
      </div>
    </div>
  </DialogHeader>
  {/* Form content (see Pattern 14) */}
</DialogContent>
```

**When to use:** Every create/edit dialog.
**When NOT to use:** Destructive (delete/cancel) dialogs — use Pattern 13.

**Common mistakes:**
- Using plain `DialogTitle` without icon box (Pattern A) — all dialogs need icons
- Using `rounded-full` icon container (Pattern C) — standard is `rounded-lg`
- Putting icon inline in `DialogTitle` (Pattern D) — use side-by-side layout
- Using `sm:max-w-[425px]` or `sm:max-w-[550px]` — standard is `sm:max-w-[500px]`
- For complex/tabbed dialogs: use `sm:max-w-[600px]`

---

## 13. Dialog Header — Destructive

**Category:** Dialog
**Use:** All delete/cancel confirmation dialogs.

```tsx
<AlertDialogContent className="border-destructive/30">
  <AlertDialogHeader>
    <div className="flex items-center gap-3">
      <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
        <AlertTriangle className="h-5 w-5 text-destructive" />
      </div>
      <div>
        <AlertDialogTitle>{t('...')}</AlertDialogTitle>
        <AlertDialogDescription>{t('...')}</AlertDialogDescription>
      </div>
    </div>
  </AlertDialogHeader>
  <AlertDialogFooter>
    <AlertDialogCancel className="cursor-pointer">
      {t('labels.cancel')}
    </AlertDialogCancel>
    <AlertDialogAction
      className="cursor-pointer bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors"
      onClick={handleDelete}
      disabled={isPending}
    >
      {isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
      {t('labels.delete')}
    </AlertDialogAction>
  </AlertDialogFooter>
</AlertDialogContent>
```

**When to use:** Every destructive action confirmation.
**When NOT to use:** Non-destructive create/edit dialogs.

**Common mistakes:**
- Forgetting `border-destructive/30` on `AlertDialogContent`
- Using `rounded-lg` on icon container — destructive uses `rounded-xl`
- Using wrong icon color (e.g., `text-amber-500`) — must be `text-destructive`
- Forgetting `Loader2` spinner on submit button — always show loading state
- Using raw `<Button>` instead of `AlertDialogCancel`/`AlertDialogAction`
- Forgetting `cursor-pointer` on both footer buttons

---

## 14. Form Dialog Layout

**Category:** Dialog
**Use:** Complete form dialog structure (create/edit entities).

```tsx
<DialogContent className="sm:max-w-[500px]">
  {/* Pattern 12 header */}
  <DialogHeader>
    <div className="flex items-center gap-3">
      <div className="p-2 bg-primary/10 rounded-lg">
        <Icon className="h-5 w-5 text-primary" />
      </div>
      <div>
        <DialogTitle>{t('...')}</DialogTitle>
        <DialogDescription>{t('...')}</DialogDescription>
      </div>
    </div>
  </DialogHeader>

  <Form {...form}>
    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
      <FormField
        control={form.control}
        name="fieldName"
        render={({ field }) => (
          <FormItem>
            <FormLabel>{t('...')}</FormLabel>
            <FormControl>
              <Input {...field} />
            </FormControl>
            <FormMessage />
          </FormItem>
        )}
      />

      <DialogFooter>
        <Button
          type="button"
          variant="outline"
          onClick={onClose}
          className="cursor-pointer"
        >
          {t('labels.cancel')}
        </Button>
        <Button
          type="submit"
          disabled={isPending}
          className="cursor-pointer"
        >
          {isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
          {t('labels.save')}
        </Button>
      </DialogFooter>
    </form>
  </Form>
</DialogContent>
```

**When to use:** Every form-based dialog.
**When NOT to use:** Display-only dialogs, destructive confirmations.

**Common mistakes:**
- Using raw `<Label>` + `<Input>` instead of shadcn `Form`/`FormField` — always use shadcn Form for validated inputs
- Wrapping form in `<ScrollArea>` or `overflow-hidden` — clips focus rings
- Forgetting `Loader2` spinner on submit button
- Forgetting `mode: 'onBlur'` on `useForm()` — required for all forms
- Forgetting `cursor-pointer` on Cancel and Submit buttons
- Text-only loading ("Saving...") without spinner icon

---

## 15. Error Banner

**Category:** Feedback
**Use:** Displaying error messages at the top of content areas.

```tsx
{error && (
  <div className="mb-4 p-4 rounded-lg bg-destructive/10 border border-destructive/20 text-destructive animate-in fade-in-0 slide-in-from-top-2 duration-300">
    <p className="text-sm font-medium">{error}</p>
  </div>
)}
```

**When to use:** API errors, mutation failures, form-level server errors.
**When NOT to use:** Field-level validation — use `<FormMessage />` instead.

**Common mistakes:**
- Forgetting the entrance animation (`animate-in fade-in-0 slide-in-from-top-2`)
- Using `bg-destructive` (solid) instead of `bg-destructive/10` (subtle)
- Placing outside `<CardContent>` — should be inside, before the main content

---

## 16. Stale/Pending Overlay

**Category:** Feedback
**Use:** Visual dimming during React 19 transitions (search, filter, pagination, tab switches).

```tsx
const [isFilterPending, startFilterTransition] = useTransition()
const deferredSearch = useDeferredValue(searchInput)
const isSearchStale = searchInput !== deferredSearch

// In JSX:
<CardContent
  className={
    isSearchStale || isFilterPending
      ? 'opacity-70 transition-opacity duration-200'
      : 'transition-opacity duration-200'
  }
>
  {/* Table or content */}
</CardContent>
```

**When to use:** All list pages during search/filter/pagination transitions. All settings tab transitions.
**When NOT to use:** Initial loading (use Pattern 10 skeleton instead).

**Common mistakes:**
- Forgetting to apply `opacity-70` — the transition var exists but CSS class isn't applied (see Shipping page bug)
- Using `opacity-50` — too aggressive, `70` is the standard
- Forgetting `transition-opacity duration-200` on both branches — causes flash

---

## 17. Pagination

**Category:** Navigation
**Use:** Page navigation below tables.

```tsx
import { Pagination } from '@uikit'

{data && data.totalPages > 1 && (
  <div className="mt-6 pt-6 border-t border-border/50">
    <Pagination
      currentPage={data.page}
      totalPages={data.totalPages}
      totalItems={data.totalCount}
      pageSize={params.pageSize || DEFAULT_PAGE_SIZE}
      onPageChange={setPage}
      showPageSizeSelector={false}
      className="justify-center"
    />
  </div>
)}
```

**Manual fallback** (if `Pagination` component isn't suitable):

```tsx
<div className="flex items-center justify-between mt-4">
  <p className="text-sm text-muted-foreground">
    {t('pagination.showing', { from, to, total })}
  </p>
  <div className="flex items-center gap-2">
    <Button
      variant="outline"
      size="sm"
      className="cursor-pointer"
      disabled={page === 1}
      onClick={() => setPage(page - 1)}
    >
      <ChevronLeft className="h-4 w-4" />
    </Button>
    <Button
      variant="outline"
      size="sm"
      className="cursor-pointer"
      disabled={page === totalPages}
      onClick={() => setPage(page + 1)}
    >
      <ChevronRight className="h-4 w-4" />
    </Button>
  </div>
</div>
```

**When to use:** All paginated tables.
**When NOT to use:** Tables that load all data at once.

**Common mistakes:**
- Building custom prev/next buttons when the `Pagination` UIKit component exists
- Forgetting `cursor-pointer` on navigation buttons
- Forgetting `disabled` state on first/last page
- Wrapping `setPage` in `startTransition` is **required** — combine with Pattern 16

---

## Global Rules (Apply to ALL Patterns)

| Rule | Details |
|------|---------|
| **`cursor-pointer`** | Required on ALL interactive elements: buttons, tabs, checkboxes, selects, switches, dropdown items |
| **Icon sizing** | Always use `h-N w-N` (not `size-N`) |
| **i18n** | All user-facing text via `t('namespace.key')` — no hardcoded English |
| **Dark mode** | Badge colors and status indicators must include `dark:` variants |
| **Form system** | Always use shadcn `Form`/`FormField` with Zod + `mode: 'onBlur'` |
| **Loading feedback** | Submit buttons must show `Loader2` spinner during `isPending` — never text-only |
| **`aria-label`** | Required on all icon-only buttons (e.g., `aria-label={t('labels.editItem', { name: item.name })}`) |

---

## File Reference

| Resource | Path |
|----------|------|
| UIKit components | `src/uikit/` (import via `@uikit`) |
| EmptyState | `import { EmptyState } from '@uikit'` |
| Pagination | `import { Pagination } from '@uikit'` |
| Skeleton | `import { Skeleton } from '@uikit'` |
| Badge | `import { Badge } from '@uikit'` |
| Form components | `import { Form, FormField, FormItem, FormLabel, FormControl, FormMessage } from '@uikit'` |
| Order status utility | `portal-app/orders/utils/orderStatus.ts` |
| Product status config | `lib/constants/product.ts` |
| Full audit report | `docs/frontend/audit-ui-patterns.md` |
