# DataTable Standard (Products Page = Reference)

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
- **Size**: 44px, non-sortable, non-hideable

## Sticky Columns

Use `meta: { sticky: 'left' }` on column definitions. `DataTable.tsx` auto-applies `sticky left-0 z-10 bg-background` to both `<th>` and `<td>`.

## Column Visibility

- Use `columnVisibilityStorageKey` in `useServerTable()` for localStorage persistence
- Pass `onResetColumnVisibility={table.resetColumnVisibility}` to `DataTableToolbar`
- Storage key format: `noir:col-vis:{page-name}` (e.g. `'customers'`, `'orders'`)
- Pages with few columns: set `showColumnToggle={false}`

## Bug Prevention

- **`useTransition`-wrapped filter callbacks**: If a filter button shows active count, track count locally (not from deferred prop). The prop updates are delayed by `startFilterTransition`.
- **Credenza stableChildrenRef**: Credenza freezes children when `open` becomes `false` (close animation). If a trigger button inside Credenza updates local state AND closes the dialog simultaneously, the button renders stale content. **Fix**: Place trigger buttons OUTSIDE `<Credenza>` with `onClick={() => setOpen(true)}` instead of using `<CredenzaTrigger>`. See `AttributeFilterDialog.tsx`.

## Reference Implementations

- **Standard DataTable**: `CustomersPage.tsx`, `OrdersPage.tsx`
- **Custom table with same pattern**: `ProductsPage.tsx` (the gold standard), `UserTable.tsx`
