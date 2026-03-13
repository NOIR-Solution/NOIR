# DataTable Migration Checklist

Pages still using custom tables (`ColumnVisibilityDropdown` + `useColumnVisibility` + raw `Table`) must be migrated to DataTable (TanStack Table). This ensures a single source of truth for column visibility and consistent UX.

**Goal:** 100/100 — All list pages use DataTable, pass UI audit (0 CRITICAL, 0 HIGH), and follow design standards.

---

## Migration Pattern

Reference: `UsersPage.tsx` (recently migrated), `CustomersPage.tsx`, `OrdersPage.tsx`, `RolesPage.tsx`.

### Before (custom table)

```tsx
const colVis = useColumnVisibility('page-key', COLUMNS)
// ...
<ColumnVisibilityDropdown columns={COLUMNS} {...colVis} />
<Table>
  <TableHeader>
    {colVis.isVisible('name') && <TableHead>...</TableHead>}
  </TableHeader>
  <TableBody>...</TableBody>
</Table>
```

### After (DataTable)

```tsx
const ch = createColumnHelper<ItemType>()
const columns = useMemo((): ColumnDef<ItemType, unknown>[] => [
  createActionsColumn<ItemType>(...),
  ch.accessor('name', { header: ..., meta: { label: t('...') }) }),
  // ...
], [deps])

const { params, searchInput, setSearchInput, ... } = useTableParams<Filters>({ defaultPageSize })
const { data } = useQuery(params)
const table = useServerTable({ data, columns, rowCount, columnVisibilityStorageKey: 'page-key', ... })

<DataTableToolbar table={table} searchInput={...} onSearchChange={...} onResetColumnVisibility={table.resetColumnVisibility} />
<DataTable table={table} ... />
<DataTablePagination table={table} />
```

---

## Pages to Migrate (Priority Order)

All list pages have been migrated. See "Pages Already Migrated" below.

---

## Pages Already Migrated (No Action)

- UsersPage, RolesPage, TenantsPage
- PromotionsPage, BrandsPage, CustomersPage, OrdersPage
- ReviewsPage, BlogPostsPage, BlogTagsPage
- BlogCategoriesPage, ProductCategoriesPage, ProductAttributesPage
- CustomerGroupsPage, DepartmentsPage, CompaniesPage, ContactsPage
- EmployeesPage, ProjectsPage, InventoryReceiptsPage, PaymentsPage
- ProductsPage, MediaLibraryPage

---

## Settings / Embedded Tables (Skip Migration)

| Component | Location | Reason |
|-----------|----------|--------|
| ProviderList | `shipping/components/ProviderList.tsx` | Settings tab — client-side filter, no pagination |
| CustomerDetailPage | orders table | Embedded in detail view |
| PaymentDetailPage | transaction/refund tables | Embedded in detail view |
| InventoryReceiptDetailDialog | line items | Dialog content |
| TaskListView, ArchivedTasksPanel | PM tasks | Kanban-adjacent, different UX |
| WebhooksSettingsTab | webhook tables | Settings tab |
| LowStockAlertsCard | dashboard widget | Small embedded table |
| ProductCategoryAttributesDialog | attribute table | Dialog content |

---

## Migration Steps (Per Page)

1. **Pre-migration**
   - [ ] Identify all filters (role, status, type, etc.) and map to `useTableParams<Filters>`.
   - [ ] List all columns and actions; note image columns for `FilePreviewTrigger`.

2. **Implement**
   - [ ] Add `useTableParams` with `defaultPageSize` and `defaultFilters` (if any).
   - [ ] Define columns with `createColumnHelper` + `meta: { label: t('...') }` for visibility dropdown.
   - [ ] Add `createActionsColumn` first (before select column) — `datatable-actions` rule requires actions first.
   - [ ] Use `createSelectColumn()` only if row selection needed (bulk actions).
   - [ ] For image columns: use `FilePreviewTrigger` per `.claude/rules/image-preview-in-lists.md`.
   - [ ] Replace query + table with `useServerTable` + `DataTable`.
   - [ ] Replace toolbar with `DataTableToolbar` (search, filters via `filterSlot`, `onResetColumnVisibility`).
   - [ ] Replace pagination with `DataTablePagination`.
   - [ ] Add `EmptyState` to `DataTable` `emptyState` prop.
   - [ ] Remove `ColumnVisibilityDropdown`, `useColumnVisibility`, `COLUMNS` arrays.

3. **TypeScript**
   - [ ] Use `params.filters.role` (not `params.role`) for filter values in Select components — `TableParams` type has filters nested.

4. **Post-migration**
   - [ ] `pnpm run build` — must pass.
   - [ ] `dotnet build src/NOIR.sln` — must pass.
   - [ ] Run UI audit: `cd e2e && npx playwright test --project=ui-audit --project=ui-audit-platform`.
   - [ ] Verify 0 CRITICAL, 0 HIGH on migrated page.

---

## 100/100 Verification Checklist

After migrating a page, verify:

| Check | Rule / Reference |
|-------|-------------------|
| Actions column first (before select) | `datatable-standard.md` — UI audit `datatable-actions` rule |
| EllipsisVertical icon (not MoreHorizontal) | `datatable-standard.md` |
| All icon-only buttons have `aria-label` | UI audit `aria-label` rule |
| All interactive elements have `cursor-pointer` | UI audit `cursor-pointer` rule |
| EmptyState component (not plain text) | UI audit `empty-state` rule |
| Card layout: `gap-0`, `pb-3`, `space-y-3` | `table-list-standard.md` |
| CardDescription with "Showing X of Y" | `table-list-standard.md` |
| Search full width (`flex-1 min-w-[200px]`) | `table-list-standard.md` |
| Status badges: `variant="outline"` + `getStatusBadgeClasses()` | `design-standards.md` |
| Image columns: `FilePreviewTrigger` | `image-preview-in-lists.md` |
| Destructive actions: confirmation dialog | CLAUDE.md Frontend Gotchas |
| `columnVisibilityStorageKey` set | `datatable-standard.md` |

---

## Cross-References

- **DataTable standard**: `.claude/rules/datatable-standard.md`
- **Table list layout**: `.claude/rules/table-list-standard.md`
- **Image preview**: `.claude/rules/image-preview-in-lists.md`
- **Design standards**: `docs/frontend/design-standards.md`
- **UI audit**: `.claude/skills/ui-audit/SKILL.md` — run before considering migration complete

---

## After All Migrations

- [ ] Deprecate or remove `ColumnVisibilityDropdown` and `useColumnVisibility`.
- [ ] Delete `UserTable.tsx` (UsersPage now uses DataTable).
- [ ] Delete `MediaTable.tsx` if MediaLibraryPage migrates to DataTable.
- [ ] Run full UI audit — target 0 CRITICAL, 0 HIGH across all 52+ pages.
