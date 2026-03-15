# DataTable Standard (TanStack Table)

## MANDATORY: Use DataTable for List Pages

All **table list pages** (paginated entity lists) MUST use TanStack Table via:
- `DataTable` + `DataTableToolbar` + `DataTablePagination` from `@uikit`
- `useEnterpriseTable` from `@/hooks/useEnterpriseTable` + `useTableParams` from `@/hooks`
- `createColumnHelper` from `@tanstack/react-table`

`useEnterpriseTable` is the unified hook — it handles both server-side state (pagination, sorting via external props) and enterprise UI state (visibility, order, sizing, pinning, density via localStorage). There is no separate `useServerTable`.

## Column Order

1. **Actions column FIRST** (leftmost) — `createActionsColumn()` from `@/lib/table/columnHelpers`
2. **Select column SECOND** (when enabled) — `createSelectColumn()`
3. **Data columns** follow

```tsx
const columns = useMemo((): ColumnDef<T, unknown>[] => [
  createActionsColumn<T>((row) => ( /* menu items */ )),
  createSelectColumn<T>(),  // only if enableRowSelection
  ch.accessor('name', { ... }),
  // ...data columns
], [deps])
```

## Actions Column Properties

- **Icon**: `EllipsisVertical` (vertical `⋮`), NOT `MoreHorizontal`
- **Position**: First column, sticky left (`meta: { sticky: 'left' }`)
- **Dropdown alignment**: `align="start"` (opens rightward)
- **Size**: **44px FIXED** — must set `size: 44, minSize: 44, maxSize: 44` to prevent resizing
- **Properties**: non-sortable, non-hideable

## Select Column Properties

- **Size**: **40px FIXED** — must set `size: 40, minSize: 40, maxSize: 40`
- **Position**: Second column (after actions)
- **Properties**: non-sortable, non-hideable

## Fixed Width Columns

DataTable automatically enforces fixed width for columns where `minSize === maxSize`. The implementation uses a `<colgroup>` with CSS Container Query units (`cqi`):

- **Fixed columns**: `width: Npx` (exact pixel width)
- **Flexible columns**: `width: calc((100cqi - fixedTotalPx) * ratio)` (proportional share of remaining space)

This ensures fixed columns (actions at 44px, select at 40px) maintain their exact widths at ALL viewport sizes. The `cqi` approach solves a fundamental CSS limitation: both `table-fixed` and `table-auto` distribute extra table width to ALL columns (including "fixed" ones) when the table is wider than the sum of column widths. `100cqi` resolves to the container's inline size, enabling exact proportional calculations that sum to the container width — leaving zero extra space to distribute.

**Implementation details**:
1. DataTable wrapper has `[container-type:inline-size]` to establish a CSS container
2. `DataTable` renders a `<colgroup>` with `<col>` elements for each column
3. Fixed columns (`minSize === maxSize`) get explicit pixel widths
4. Flexible columns get `calc((100cqi - fixedTotalPx) * colSize / flexTotal)` — proportional width from remaining space
5. The underlying `<table>` uses `table-layout: fixed` (via `Table.tsx`)

**Creating fixed-width columns**:
```tsx
// Actions column - 44px fixed
createActionsColumn<T>((row) => (...))  // Already configured: size: 44, minSize: 44, maxSize: 44

// Select column - 40px fixed
createSelectColumn<T>()  // Already configured: size: 40, minSize: 40, maxSize: 40

// Custom fixed column
{
  accessorKey: 'status',
  size: 120,
  minSize: 120,
  maxSize: 120,  // minSize === maxSize makes it fixed
}
```

## Sticky Columns

Use `meta: { sticky: 'left' }` on column definitions. `DataTable.tsx` auto-applies `sticky left-0 z-10 bg-background` to both `<th>` and `<td>`.

## Column Visibility & Enterprise Settings

- Use `tableKey` in `useEnterpriseTable()` for localStorage persistence of all enterprise settings (visibility, order, sizing, pinning, density)
- Pass enterprise toolbar props: `columnOrder={settings.columnOrder}`, `onColumnsReorder`, `isCustomized`, `onResetSettings={resetToDefault}`, `density={settings.density}`, `onDensityChange={setDensity}`
- Storage key format: `'customers'`, `'orders'` (hook prefixes automatically)
- Pages with few columns: set `showColumnToggle={false}`
- Every column MUST have `meta: { label: t('...') }` for the Columns dropdown

## Image Columns

For columns displaying thumbnails/photos, use `FilePreviewTrigger` per `.claude/rules/image-preview-in-lists.md`:
```tsx
cell: ({ row }) => row.original.imageUrl ? (
  <FilePreviewTrigger file={{ url: row.original.imageUrl, name: row.original.name }} thumbnailWidth={48} thumbnailHeight={48} />
) : null
```

## Empty State

DataTable MUST receive an `emptyState` prop with `<EmptyState icon={...} title={...} description={...} />` — never plain text. UI audit `empty-state` rule enforces this.

## TypeScript: Filter Values

`useTableParams<{ role?: string }>()` returns `params` where filter values live in `params.filters`, not at top level. Use `params.filters.role` (not `params.role`) in Select components.

## Accessibility

- All icon-only action buttons: `aria-label={t('...')}` or contextual label (e.g. `aria-label={t('labels.actions', 'Actions')}`)
- All interactive elements: `cursor-pointer` (Tabs, Checkbox, Select, Switch, etc.)

## Centered Column Alignment (Actions, Select)

Columns with `meta: { align: 'center' }` (actions, select) get special handling in DataTable:
- `<th>` and `<td>` padding is set to `0` — the inner flex wrapper handles centering
- Content is wrapped in `<div className="flex h-full w-full items-center justify-center">`
- This prevents the `[&:has([role=checkbox])]:pr-0` rule in `TableCell` from creating asymmetric padding (left pad kept, right pad removed → off-center checkbox)

**Never** add manual padding/alignment to actions or select column cells — DataTable handles it automatically via `meta.align`.

## Pagination Spacing (MANDATORY)

`CardContent` wrapping `<DataTable>` and `<DataTablePagination>` **MUST** include `space-y-3` to ensure 12px gap between table and pagination — matching the gap between toolbar and table.

```tsx
// CORRECT — space-y-3 always present, even with opacity transitions
<CardContent className={(isSearchStale || isFilterPending)
  ? 'space-y-3 opacity-70 transition-opacity duration-200'
  : 'space-y-3 transition-opacity duration-200'}>

// ALSO CORRECT — simple case
<CardContent className="space-y-3">
```

**Bug prevented**: Without `space-y-3`, pagination sits flush against the table bottom border (0px gap) while the toolbar-to-table gap is 12px — visually inconsistent.

## Page Size Selector & Persistence

`DataTablePagination` includes a built-in `PageSizeSelector` (Popover-based) with options: Default (page-specific), 20, 50, 100, and Custom (max 500).

**Required wiring:**
1. `useTableParams` — pass `tableKey` and `defaultPageSize` to enable localStorage persistence:
   ```tsx
   const { params, ..., defaultPageSize } = useTableParams<Filters>({
     defaultPageSize: 20, tableKey: 'users'
   })
   ```
2. `DataTablePagination` — pass `defaultPageSize` so the selector knows what "Default" means:
   ```tsx
   <DataTablePagination table={table} defaultPageSize={defaultPageSize} />
   ```
3. `tableKey` in `useTableParams` MUST match `tableKey` in `useEnterpriseTable` for the same page.

**Storage**: `noir:table-page-size:{tableKey}` in localStorage. Cleared when user selects Default (avoids stale entries).

**Load-all pages** (BlogTags, BlogCategories, ProductCategories): Do NOT pass `defaultPageSize` — they fetch all items client-side and don't need page size selection.

## Grouping

`useEnterpriseTable` supports `enableGrouping: true` (opt-in per page). Add `enableGrouping: true` + `aggregationFn` + `aggregatedCell` to column definitions that should be groupable.

**CRITICAL**: `groupedColumnMode` MUST be `false` (set in `useEnterpriseTable`). The default `'reorder'` causes `getVisibleLeafColumns()` to reorder columns but NOT `getHeaderGroups()` when `columnOrder` is set — colgroup widths mismatch with headers → column overlap. Never change this setting.

**`expandAllGroups`**: Sets `expanded: true` (TanStack sentinel). Type is `true | Record<string, boolean>`. Do NOT use `{}` — that means "all collapsed".

## Bug Prevention

- **`useTransition`-wrapped filter callbacks**: If a filter button shows active count, track count locally (not from deferred prop). The prop updates are delayed by `startFilterTransition`.
- **Credenza stableChildrenRef**: Credenza freezes children when `open` becomes `false` (close animation). If a trigger button inside Credenza updates local state AND closes the dialog simultaneously, the button renders stale content. **Fix**: Place trigger buttons OUTSIDE `<Credenza>` with `onClick={() => setOpen(true)}` instead of using `<CredenzaTrigger>`. See `AttributeFilterDialog.tsx`.

## Reference Implementations

- **Gold standard**: `UsersPage.tsx`, `CustomersPage.tsx`, `OrdersPage.tsx`
- **With filters**: `PromotionsPage.tsx`, `UsersPage.tsx`
- **With row selection**: `ReviewsPage.tsx`, `BlogPostsPage.tsx`, `CustomersPage.tsx`
- **With grouping**: `OrdersPage.tsx` (status), `CustomersPage.tsx` (segment, tier), `EmployeesPage.tsx` (department, status, position)
- **Storybook**: `Storybook > DataTable` — full example with actions, select, toolbar, pagination
