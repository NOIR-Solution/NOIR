# Enterprise DataTable — Phase 3 + 5 + Row Animation Workflow

> **Created**: 2026-03-14
> **Scope**: Phase 3 (Grouping & Aggregation), Phase 5 (Polish & Performance), Row Animation (new)
> **Decision**: Phase 4 (Range Selection, Clipboard, Undo/Redo, Excel Export) **REMOVED** — low ROI for current use cases
> **Predecessor**: Phases 1-2 complete (useEnterpriseTable, column management, drag-reorder, density, pinning, resizing)

---

## Phase Overview

| Phase | Name | Deliverables | Estimated Complexity |
|-------|------|-------------|---------------------|
| **3A** | Row Animation | Highlight on create/update, fade-out on delete | Low (hook exists, wire into DataTable + pages) |
| **3B** | Row Grouping | "Group by" UI, expandable groups, aggregation | High (new TanStack feature, toolbar UI, persistence) |
| **5** | Polish & Performance | Virtualization, keyboard nav, Storybook docs | Medium (optional per-page, progressive enhancement) |

---

## Phase 3A: Row Animation (useRowHighlight)

### Why First
- Hook already exists (`useRowHighlight.ts`) — just needs wiring
- Independent of grouping — works with current flat tables
- Immediate UX win across all 23 DataTable pages
- Low risk, high visibility

### Implementation Steps

#### Step 1: CSS Keyframes
**File**: `src/NOIR.Web/frontend/src/index.css`

Add animation keyframes:
```css
@keyframes row-highlight {
  0% { background-color: oklch(var(--highlight) / 0.3); }
  100% { background-color: transparent; }
}

@keyframes row-fadeout {
  0% { opacity: 1; transform: translateX(0); }
  100% { opacity: 0; transform: translateX(-8px); }
}

.animate-row-highlight {
  animation: row-highlight 1.5s ease-out;
}

.animate-row-fadeout {
  animation: row-fadeout 0.4s ease-in forwards;
}
```

#### Step 2: DataTable — Accept `getRowClassName` Prop
**File**: `src/uikit/data-table/DataTable.tsx`

Add optional prop to DataTable:
```tsx
interface DataTableProps<TData> {
  // ... existing props
  /** Optional callback to add CSS classes to a row based on row data */
  getRowClassName?: (row: Row<TData>) => string
}
```

Apply in `<tr>`:
```tsx
<TableRow
  className={cn(
    'cursor-pointer',
    getRowClassName?.(row),
  )}
>
```

#### Step 3: Wire into Pages (23 pages)
**Pattern per page:**
```tsx
const { highlightRow, fadeOutRow, getRowAnimationClass } = useRowHighlight()

// In create mutation onSuccess:
onSuccess: (data) => {
  queryClient.invalidateQueries({ queryKey: entityKeys.all })
  highlightRow(data.id)
}

// In delete handler (before mutation):
const handleDelete = async (id: string) => {
  await fadeOutRow(id)
  deleteMutation.mutate(id)
}

// Pass to DataTable:
<DataTable
  table={table}
  getRowClassName={(row) => getRowAnimationClass(row.id)}
/>
```

**Pages to wire** (all 23 DataTable pages):
1. UsersPage, RolesPage, TenantsPage
2. CustomersPage, CustomerGroupsPage
3. OrdersPage, PaymentsPage, InventoryReceiptsPage
4. ProductsPage, BrandsPage, PromotionsPage, ReviewsPage
5. ProductAttributesPage, ProductCategoriesPage, BlogCategoriesPage
6. BlogPostsPage, BlogTagsPage
7. EmployeesPage, DepartmentsPage
8. ContactsPage, CompaniesPage
9. ProjectsPage, MediaLibraryPage

**Note**: Not all pages have create/delete mutations visible on the list page. Only wire where the mutation is triggered from the list page itself (e.g., inline delete button, create dialog that returns to list).

#### Step 4: Commit `useRowHighlight.ts`
The file already exists as untracked. Just `git add` it.

### Checkpoint 3A
- [ ] CSS keyframes added and visible in browser
- [ ] DataTable accepts `getRowClassName`
- [ ] At least 1 reference page (UsersPage) wired and verified
- [ ] All 23 pages wired
- [ ] `pnpm run build` passes
- [ ] Playwright screenshot verification

---

## Phase 3B: Row Grouping & Aggregation

### Dependencies
- TanStack Table `getGroupedRowModel` (already in `@tanstack/react-table`)
- `aggregationHelpers.tsx` (recreate from spec — was previously scaffolded)
- Toolbar "Group by" UI control
- Persistence of grouping state in `useEnterpriseTable`

### Implementation Steps

#### Step 1: Enable Grouping in useEnterpriseTable
**File**: `src/hooks/useEnterpriseTable.ts`

Add to hook options:
```tsx
enableGrouping?: boolean  // default: false (opt-in per page)
```

Add to TanStack Table config:
```tsx
import { getGroupedRowModel, getExpandedRowModel } from '@tanstack/react-table'

// In useReactTable config:
enableGrouping: options.enableGrouping,
getGroupedRowModel: options.enableGrouping ? getGroupedRowModel() : undefined,
getExpandedRowModel: options.enableGrouping ? getExpandedRowModel() : undefined,
```

Persist grouping state in `EnterpriseTableSettings`:
```tsx
interface EnterpriseTableSettings {
  // ... existing
  grouping: string[]      // Column IDs to group by
  expanded: true | Record<string, boolean>  // Expanded group state
}
```

**Storage version bump**: 3 → 4 (add migration in `enterpriseSettingsStorage.ts`)

#### Step 2: "Group by" Dropdown in Toolbar
**New File**: `src/uikit/data-table/DataTableGroupBy.tsx`

Popover with:
- List of groupable columns (columns with `enableGrouping: true` in column def)
- Checkbox to toggle grouping per column
- Clear All button
- Shows current grouping state

Add to `DataTableToolbar`:
```tsx
{table.options.enableGrouping && (
  <DataTableGroupBy table={table} />
)}
```

#### Step 3: Expandable Group Rows in DataTable
**File**: `src/uikit/data-table/DataTable.tsx`

Detect grouped rows and render differently:
```tsx
{row.getIsGrouped() ? (
  // Group header row — expandable, shows aggregated values
  <TableRow className="bg-muted/50 font-medium">
    <TableCell colSpan={visibleColumns.length}>
      <button onClick={row.getToggleExpandedHandler()}>
        {row.getIsExpanded() ? <ChevronDown /> : <ChevronRight />}
        {row.groupingColumnId}: {row.groupingValue} ({row.subRows.length})
      </button>
      {/* Aggregated cells inline */}
    </TableCell>
  </TableRow>
) : (
  // Regular data row
  <TableRow>...</TableRow>
)}
```

#### Step 4: Aggregation Helpers
**New File**: `src/lib/table/aggregationHelpers.tsx`

Recreate from spec with renderers:
- `aggregatedCells.count()` — "12 items"
- `aggregatedCells.currency()` — "$15,000.00"
- `aggregatedCells.sum()` — numeric total
- `aggregatedCells.average()` — "avg: 4.5"
- `aggregatedCells.range()` — "100 – 500"
- `weightedAverage()` — custom aggregation function

#### Step 5: Wire into Reference Pages
Start with pages that benefit most from grouping:

| Page | Group By Candidates | Aggregation |
|------|-------------------|-------------|
| **OrdersPage** | Status, PaymentStatus | Sum(Total), Count |
| **CustomersPage** | Tier, Segment | Sum(TotalSpent), Count, Avg(Orders) |
| **EmployeesPage** | Department, Status | Count per department |
| **PaymentsPage** | Status, Method | Sum(Amount), Count |
| **ProductsPage** | Status, Category | Count, Avg(Price) |

Add `enableGrouping: true` to these pages' `useEnterpriseTable` call and add `aggregationFn` + `aggregatedCell` to relevant columns.

### Checkpoint 3B
- [ ] `useEnterpriseTable` supports `enableGrouping`
- [ ] Storage version migrated (v3 → v4)
- [ ] "Group by" dropdown in toolbar
- [ ] Group rows expand/collapse
- [ ] Aggregation renders correctly
- [ ] At least 5 pages wired with grouping
- [ ] Grouping state persists across page reload
- [ ] `pnpm run build` passes
- [ ] Server-side grouping considered (defer if not needed)

---

## Phase 5: Polish & Performance

### Step 1: Virtual Scrolling (Optional, Per-Page)
**New File**: `src/uikit/data-table/DataTableVirtual.tsx`

Use `@tanstack/react-virtual` for pages with 100+ rows:
```tsx
import { useVirtualizer } from '@tanstack/react-virtual'
```

This is a **separate component** — not a modification of DataTable. Pages opt-in:
```tsx
<DataTableVirtual table={table} height={600} />
// instead of
<DataTable table={table} />
```

**Candidates**: Pages where users may set page size to 100-500 (all pages with PageSizeSelector).

### Step 2: Keyboard Navigation
**File**: `src/uikit/data-table/DataTable.tsx`

Add keyboard event handlers:
| Key | Action |
|-----|--------|
| `Arrow Up/Down` | Move focus between rows |
| `Enter` | Open row actions / navigate to detail |
| `Space` | Toggle row selection |
| `Home/End` | Jump to first/last row |
| `Tab` | Move between focusable cells |

Implementation: `useKeyboardNavigation` hook with `tabIndex={0}` on table body, `aria-activedescendant` for screen readers.

### Step 3: Mobile Touch Improvements
**File**: `src/uikit/data-table/DataTable.tsx`

- Swipe to reveal actions (on mobile viewports)
- Touch-friendly resize handles (larger hit area)
- Pinch-to-zoom disabled on table (prevents accidental zoom)

### Step 4: Storybook Documentation
**New Files**:
- `src/uikit/data-table/DataTable.stories.tsx` — already exists, enhance with:
  - Grouping story
  - Aggregation story
  - Virtual scrolling story
  - Row animation story
  - Keyboard navigation demo

### Step 5: E2E Test Suite
**File**: `src/NOIR.Web/frontend/e2e/datatable.spec.ts`

Test scenarios:
- Column reorder persistence
- Density toggle persistence
- Page size selector + persistence
- Group by + expand/collapse
- Row highlight animation (visual regression)
- Keyboard navigation
- Mobile responsive

### Checkpoint 5
- [ ] Virtual scrolling works with 500 rows
- [ ] Keyboard navigation functional
- [ ] Mobile touch improvements
- [ ] Storybook stories updated
- [ ] E2E tests pass
- [ ] `pnpm run build` passes

---

## Execution Order

```
Phase 3A: Row Animation          ←── START HERE (low effort, high impact)
  ↓
Phase 3B: Grouping & Aggregation ←── Main feature work
  ↓
Phase 5: Polish & Performance    ←── Progressive enhancement
```

**Phase 4 REMOVED**: Range selection, clipboard, undo/redo, Excel export are enterprise-grade features with high implementation cost and low usage frequency for current NOIR use cases. Can be revisited if user demand arises.

---

## Updated Build Spec Phases

| Phase | Name | Status |
|-------|------|--------|
| 1 | Foundation | **COMPLETE** (2026-03-13) |
| 2 | Column Management | **COMPLETE** (2026-03-13) |
| 3A | Row Animation | **COMPLETE** (2026-03-14) |
| 3B | Grouping & Aggregation | **COMPLETE** (2026-03-14) |
| ~~4~~ | ~~Advanced Features~~ | **REMOVED** |
| 5 | Polish & Performance | **COMPLETE** (2026-03-14) |

---

## Quality Gates (After Each Phase)

```bash
pnpm run build                    # 0 errors
pnpm build-storybook              # 0 errors
dotnet build src/NOIR.sln         # 0 errors (if backend changes)
cd e2e && npx playwright test --project=ui-audit  # 0 CRITICAL, 0 HIGH
```

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|-----------|
| Grouping breaks server-side pagination | Medium | High | Grouping is client-side only initially; server-side deferred |
| Performance with 500 grouped rows | Low | Medium | Virtual scrolling in Phase 5 |
| Storage migration breaks existing settings | Low | Medium | Version check + graceful fallback |
| Row animation conflicts with table transitions | Low | Low | CSS specificity; animation is additive |
