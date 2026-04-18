---
name: noir-datatable-page
description: Scaffold a new DataTable list page in NOIR following the enterprise table standard (useEnterpriseTable + DataTable + DataTableToolbar + DataTablePagination + audit columns). Use when the user asks to create, build, or add a new list page, admin page, index page, or table view. Covers `.claude/rules/datatable-standard.md`, `table-list-standard.md`, and `audit-columns-standard.md`.
---

# noir-datatable-page — Enterprise list page scaffold

NOIR has a strict table list standard: TanStack Table + localStorage-persisted settings + actions column first + 4 audit columns last + card layout with `gap-0` / `pb-3` / `space-y-3`. This skill walks the scaffold end-to-end.

## Prerequisites

- Backend list query exists: `Get{Entity}sQuery` in `src/NOIR.Application/Features/{Feature}/Queries/Get{Entity}s/`
- List DTO includes audit fields: `CreatedAt`, `ModifiedAt`, `CreatedByName`, `ModifiedByName` (user names resolved via `IUserDisplayNameService.GetDisplayNamesAsync()` — batch, not per-row)
- List specification supports sort cases for `createdby`/`creator` and `modifiedby`/`editor` (see `audit-columns-standard.md`)
- Query hook exists in `src/queries/use{Feature}Queries.ts`

If any are missing, invoke `noir-feature-add` first.

## Inputs to collect

1. **Entity / feature name** — `Product`, `Customer`, `Brand`
2. **Visible columns** — domain data (name, status, price) + any image/thumbnail columns
3. **Filters** — status dropdown, type dropdown, date range, etc.
4. **Row selection / bulk actions?** — yes/no
5. **Grouping?** — e.g. by status, by category (optional)
6. **Actions per row** — view, edit, delete, duplicate, specific domain actions

## Reference implementations (read before writing — Rule 1)

- **Gold standard**: `src/portal-app/access/users/UsersPage.tsx`
- **With filters**: `src/portal-app/marketing/promotions/PromotionsPage.tsx`
- **With grouping**: `src/portal-app/ecommerce/orders/OrdersPage.tsx`
- **With row selection + bulk ops**: `src/portal-app/content/blog/BlogPostsPage.tsx`

## Skeleton

```tsx
import { useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { createColumnHelper, type ColumnDef } from '@tanstack/react-table'
import { Plus, Eye, Edit, Trash2 } from 'lucide-react'
import { useEnterpriseTable } from '@/hooks/useEnterpriseTable'
import { useTableParams } from '@/hooks/useTableParams'
import { useUrlDialog, useUrlEditDialog } from '@/hooks'
import { useBrandsQuery, useDeleteBrand } from '@/queries/useBrandQueries'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import {
  Card, CardContent, CardHeader, CardTitle, CardDescription,
  DataTable, DataTableToolbar, DataTablePagination,
  DropdownMenuItem, EmptyState, Button,
} from '@uikit'
import { PageHeader } from '@/components/portal/PageHeader'
import {
  createActionsColumn, createSelectColumn, createFullAuditColumns,
} from '@/lib/table/columnHelpers'
import type { BrandDto } from '@/types/brand'
import { BrandDialog } from './BrandDialog'
import { DeleteBrandDialog } from './DeleteBrandDialog'
import { Building2 } from 'lucide-react'

interface BrandFilters {
  isActive?: boolean
}

export const BrandsPage = () => {
  const { t } = useTranslation()
  const { formatDateTime } = useRegionalSettings()

  const {
    params, defaultPageSize,
    searchInput, setSearchInput,
    onPaginationChange, onSortingChange,
  } = useTableParams<BrandFilters>({ defaultPageSize: 20, tableKey: 'brands' })

  const { data, isLoading } = useBrandsQuery(params)

  // URL-synced dialogs
  const { isOpen: isCreateOpen, open: openCreate, onOpenChange: onCreateOpenChange } = useUrlDialog({ paramValue: 'create-brand' })
  const { editItem, openEdit, onEditOpenChange } = useUrlEditDialog<BrandDto>(data?.items ?? [])
  const [deleteTarget, setDeleteTarget] = useState<BrandDto | null>(null)
  const deleteMutation = useDeleteBrand()

  // Columns — actions first (44px), select second (40px), data, then audit columns last
  const ch = createColumnHelper<BrandDto>()
  const columns = useMemo<ColumnDef<BrandDto, unknown>[]>(() => [
    createActionsColumn<BrandDto>((row) => (
      <>
        <DropdownMenuItem onClick={() => openEdit(row)} className="cursor-pointer">
          <Edit className="mr-2 h-4 w-4" />{t('buttons.edit')}
        </DropdownMenuItem>
        <DropdownMenuItem onClick={() => setDeleteTarget(row)} className="cursor-pointer text-destructive">
          <Trash2 className="mr-2 h-4 w-4" />{t('buttons.delete')}
        </DropdownMenuItem>
      </>
    )),
    createSelectColumn<BrandDto>(),
    ch.accessor('name', {
      header: t('brands.name'),
      meta: { label: t('brands.name') },
      cell: ({ row }) => <span className="font-medium">{row.original.name}</span>,
    }),
    ch.accessor('isActive', {
      header: t('labels.status'),
      enableGrouping: true,
      meta: {
        label: t('labels.status'),
        groupValueFormatter: (v) => v ? t('statuses.active') : t('statuses.inactive'),
      },
      cell: ({ getValue }) => (
        <Badge variant="outline" className={getStatusBadgeClasses(getValue() ? 'green' : 'gray')}>
          {getValue() ? t('statuses.active') : t('statuses.inactive')}
        </Badge>
      ),
    }),
    ...createFullAuditColumns<BrandDto>(t, formatDateTime),
  ], [t, formatDateTime, openEdit])

  const { table, settings, isCustomized, resetToDefault, setDensity } = useEnterpriseTable({
    data: data?.items ?? [],
    columns,
    tableKey: 'brands',
    rowCount: data?.totalCount ?? 0,
    state: {
      pagination: { pageIndex: params.pageIndex, pageSize: params.pageSize },
      sorting: params.sorting,
    },
    onPaginationChange,
    onSortingChange,
    enableRowSelection: true,
    getRowId: (row) => row.id,
  })

  return (
    <>
      <PageHeader
        title={t('brands.title')}
        description={t('brands.description')}
        action={
          <Button onClick={() => openCreate()} className="group transition-all duration-300">
            <Plus className="mr-2 h-4 w-4" />{t('brands.createBrand')}
          </Button>
        }
      />

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300 gap-0">
        <CardHeader className="pb-3">
          <div className="space-y-3">
            <div>
              <CardTitle className="text-lg">{t('brands.allBrands')}</CardTitle>
              <CardDescription>
                {data ? t('labels.showingCountOfTotal', { count: data.items.length, total: data.totalCount }) : ''}
              </CardDescription>
            </div>
            <DataTableToolbar
              table={table}
              searchInput={searchInput}
              onSearchChange={setSearchInput}
              searchPlaceholder={t('brands.searchPlaceholder')}
              columnOrder={settings.columnOrder}
              onColumnsReorder={(newOrder) => table.setColumnOrder(newOrder)}
              isCustomized={isCustomized}
              onResetSettings={resetToDefault}
              density={settings.density}
              onDensityChange={setDensity}
            />
          </div>
        </CardHeader>
        <CardContent className="space-y-3">
          <DataTable
            table={table}
            density={settings.density}
            isLoading={isLoading}
            emptyState={
              <EmptyState
                icon={Building2}
                title={t('brands.noBrandsFound')}
                description={t('brands.noBrandsFoundDescription')}
              />
            }
          />
          <DataTablePagination table={table} defaultPageSize={defaultPageSize} />
        </CardContent>
      </Card>

      {/* Create + Edit combined dialog — conditional close (see url-tab-state.md) */}
      <BrandDialog
        open={isCreateOpen || !!editItem}
        onOpenChange={(open) => {
          if (!open) {
            if (isCreateOpen) onCreateOpenChange(false)
            if (editItem) onEditOpenChange(false)
          }
        }}
        brand={editItem}
      />

      <DeleteBrandDialog
        brand={deleteTarget}
        open={!!deleteTarget}
        onOpenChange={(open) => !open && setDeleteTarget(null)}
      />
    </>
  )
}
```

## Non-negotiables (cross-cutting rules)

- **Column order**: Actions (44px fixed, `EllipsisVertical`, `align="start"`) → Select (40px fixed) → data columns → `createFullAuditColumns(t, formatDateTime)` at end
- **Card layout**: `gap-0` + `pb-3` header + `space-y-3` content (critical — pagination sits flush without it)
- **CardDescription**: always `Showing X of Y items` via `labels.showingCountOfTotal`
- **Search**: `flex-1 min-w-[200px]` — never max-width
- **`tableKey`**: matches between `useTableParams` and `useEnterpriseTable` — enables localStorage persistence
- **Image columns**: use `FilePreviewTrigger` (`image-preview-in-lists.md`)
- **Grouping**: set `groupedColumnMode: false` (already the default in `useEnterpriseTable` — never change it)
- **Group values**: enum columns need `meta.groupValueFormatter` for i18n
- **Date display**: `formatDateTime` from `useRegionalSettings()` — never `toLocaleString()` / `formatRelativeTime` (`date-formatting.md`)
- **Interactive elements**: all have `cursor-pointer`
- **Icon-only buttons**: all have `aria-label`
- **Destructive actions**: confirmation dialog (`DeleteXxxDialog`) — never direct `onClick={() => delete()}`

## After-scaffold checklist

- [ ] `pnpm run build` — strict mode passes
- [ ] `cd e2e && npx playwright test --project=ui-audit` — **0 CRITICAL, 0 HIGH** on the new page
- [ ] Browser test:
   - [ ] Header Card has 12px gap to content (not 0, not 24)
   - [ ] Pagination has 12px gap above (space-y-3)
   - [ ] Columns Reorder via toolbar dropdown persists after reload
   - [ ] Density change persists after reload
   - [ ] Column visibility persists after reload
   - [ ] Create dialog opens via URL param (`?dialog=create-brand`)
   - [ ] Edit dialog opens via URL param (`?edit={id}`)
   - [ ] Delete asks for confirmation
   - [ ] Empty state shows EmptyState component (not plain text)
   - [ ] All icon buttons have tooltips / aria-labels
- [ ] i18n keys in BOTH en and vi (`brands.*`, `labels.*`, `statuses.*`)
- [ ] Audit columns visible: Created At + Creator (default), Modified At + Editor (hidden, toggle via Columns dropdown)

## Common mistakes this skill prevents

- Custom table with `<table>` + `ColumnVisibilityDropdown` instead of DataTable (pre-2026-03-13 pattern — forbidden)
- Missing `gap-0` on Card → 24px gap between header and content (default)
- Missing `pb-3` on CardHeader → 0px gap (too tight)
- Missing `space-y-3` on CardContent → pagination flush against table (inconsistent with above-table gap)
- Search input with `max-w-[280px]` → narrow on wide screens
- Actions column NOT first → UI audit `datatable-actions` rule fails
- `MoreHorizontal` icon instead of `EllipsisVertical` → rule violation
- Missing audit columns → `audit-columns-standard.md` violation
- Using `toLocaleString()` or `formatRelativeTime()` in columns → `date-formatting.md` violation (tables always use `formatDateTime`)
- Missing `tableKey` → no localStorage persistence (user's column order resets every reload)
- `tableKey` differs between `useTableParams` and `useEnterpriseTable` → two separate localStorage keys, partially persisted
- Missing `getRowId` → selection state breaks on re-fetch
- `groupedColumnMode: 'reorder'` (default if not set) → columns overlap when grouping + column order both active
- Enum columns without `meta.groupValueFormatter` → grouped rows show English values in Vietnamese UI
- `handleSubmit` in filter Select causing re-render loop — filter Select uses `params.filters.role`, not `params.role`
- Plain div for empty state instead of `<EmptyState>` → UI audit fails
- Destructive action without confirmation dialog → accidental data loss
