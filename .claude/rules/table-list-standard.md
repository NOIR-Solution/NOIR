# Table List Page Standard (Users Page = Reference)

## MANDATORY: Use DataTable (TanStack)

Every table list page MUST use `DataTable` + `useEnterpriseTable` — see `.claude/rules/datatable-standard.md`. No custom tables with `ColumnVisibilityDropdown` + `useColumnVisibility`.

## Card Layout

Every table list page MUST use this Card structure:

```tsx
<Card className="shadow-sm hover:shadow-lg transition-all duration-300 gap-0">
  <CardHeader className="pb-3">
    <div className="space-y-3">
      <div>
        <CardTitle className="text-lg">{t('domain.allItems', 'All Items')}</CardTitle>
        <CardDescription>
          {data ? t('labels.showingCountOfTotal', { count: data.items.length, total: data.totalCount }) : ''}
        </CardDescription>
      </div>
      <DataTableToolbar table={table} searchInput={...} onSearchChange={...} ... />
    </div>
  </CardHeader>
  <CardContent className="space-y-3">
    <DataTable table={table} ... />
    <DataTablePagination table={table} defaultPageSize={defaultPageSize} />
  </CardContent>
</Card>
```

### Key Rules

1. **`gap-0`** on Card — overrides default `gap-6` (24px) between header and content
2. **`pb-3`** on CardHeader — provides 12px spacing between header and content
3. **`space-y-3`** on CardContent — 12px between DataTable and DataTablePagination (NOT `space-y-4`). **CRITICAL**: When using conditional opacity (`isSearchStale ? '...' : '...'`), `space-y-3` MUST appear in BOTH branches of the ternary. Missing it causes pagination to sit flush against the table (0px gap).
4. **CardDescription** REQUIRED — always show `Showing X of Y items` count
5. **CardTitle** — always `className="text-lg"`, uses `t('domain.allItems')` pattern

### Showing Count

- Paginated data: `{data ? t('labels.showingCountOfTotal', { count: data.items.length, total: data.totalCount }) : ''}`
- Flat array data: `{t('labels.showingCountOfTotal', { count: data.length, total: data.length })}`
- Translation key: `labels.showingCountOfTotal` (already exists in both EN and VI)

## Search Input

Search MUST take full width (`flex-1 min-w-[200px]`). NEVER add `max-w-[280px]` or similar constraints.

```tsx
{/* Full-width search with optional filter dropdowns */}
<div className="flex flex-wrap items-center gap-2">
  <div className="relative flex-1 min-w-[200px]">
    <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
    <Input placeholder={t('...')} className="pl-9 h-9" />
  </div>
  {/* Filter Select dropdowns: w-[140px] h-9 cursor-pointer */}
</div>
```

## Column Order (also in `datatable-standard.md`)

1. **Actions** FIRST (sticky left, `EllipsisVertical` icon, dropdown `align="start"`)
2. **Select/Checkbox** SECOND (when enabled)
3. **Data columns** follow

Every table SHOULD have a select/checkbox column for consistency.

## Post-Migration Verification (100/100)

After migrating or creating a table list page:

1. **Build**: `pnpm run build` and `dotnet build src/NOIR.sln` — must pass
2. **UI Audit**: `cd src/NOIR.Web/frontend/e2e && npx playwright test --project=ui-audit --project=ui-audit-platform`
3. **Target**: 0 CRITICAL, 0 HIGH on the migrated page

See `.claude/rules/datatable-migration.md` for full 100/100 verification checklist.

## Reference Implementations

- **Gold standard**: `UsersPage.tsx` — DataTableToolbar in CardHeader, filters, column visibility
- **With filters**: `PromotionsPage.tsx`, `UsersPage.tsx`
- **With row selection**: `ReviewsPage.tsx`, `CustomersPage.tsx`
