# NOIR Enterprise DataTable - Build Specification

> **Goal**: Match or exceed AG Grid Enterprise features
> **Research Date**: 2026-03-13
> **Status**: Phases 1-3, 5 Complete. Phase 4 removed. Enterprise DataTable feature-complete.

---

## Executive Summary

After comprehensive research into AG Grid, TanStack Table, MUI X Data Grid, and Glide Data Grid, this specification outlines how to build an enterprise data table that **matches AG Grid's core features** while **exceeding in DX, bundle size, and integration**.

### Key Competitive Advantages

| Aspect | AG Grid Enterprise | NOIR DataTable (Target) |
|--------|-------------------|------------------------|
| **Bundle Size** | ~500KB (AG Grid) + ~300KB (Enterprise) | ~150KB (TanStack + our code) |
| **Framework** | Framework-agnostic (overhead) | React-optimized |
| **Learning Curve** | Steep (separate API) | Native TanStack (familiar) |
| **Server-Side** | Complex (requires adapters) | Native support |
| **Customization** | Good | Superior (full React control) |
| **Tree Shaking** | Limited | Excellent |
| **Price** | $1,000+/year | Free/Open Source |

---

## Part 1: Feature Gap Analysis

### 1.1 AG Grid Features We MUST Match

#### Core Features (100% Required)

| Feature | AG Grid Impl | Our Strategy | Risk |
|---------|-------------|--------------|------|
| **Column Pinning** | CSS sticky + scroll sync | TanStack native + CSS | Low |
| **Column Resizing** | `onChange` + ghost element | `onEnd` mode + CSS vars | Low |
| **Column Reorder** | Drag header | Dropdown DnD (reliable) | Low |
| **Row Grouping** | Client + Server | TanStack `getGroupedRowModel` | Med |
| **Aggregation** | Auto-calc per column | `aggregationFn` native | Low |
| **Row Selection** | Checkbox + Range | TanStack native + custom | Low |
| **Sorting** | Multi-column | TanStack native | None |
| **Filtering** | Column + Global | TanStack `getFilteredRowModel` | Low |

#### Premium Features (AG Grid Enterprise $$$)

| Feature | Priority | Implementation | Value |
|---------|----------|----------------|-------|
| **Range Selection** (Excel-like) | HIGH | Custom hook + CSS | Differentiator |
| **Master-Detail** | MEDIUM | Expandable rows | Match |
| **Tree Data** | MEDIUM | Hierarchical rows | Match |
| **Clipboard** | HIGH | Copy/paste cells | Differentiator |
| **Excel Export** | HIGH | xlsx.js integration | Match |
| **CSV Export** | MEDIUM | Native | Match |
| **Print Layout** | LOW | CSS @media print | Nice-to-have |
| **Undo/Redo** | MEDIUM | Command pattern | Differentiator |
| **Row Drag** | MEDIUM | @dnd-kit | Match |

### 1.2 Features We Can EXCEED AG Grid

| Feature | AG Grid Limitation | Our Advantage |
|---------|-------------------|---------------|
| **Server-Side** | Requires complex adapter | Native TanStack support |
| **React Integration** | Wrapper components | Native React hooks |
| **Customization** | Template system | Full React render props |
| **Bundle Size** | 800KB+ | <200KB |
| **TypeScript** | Good | Excellent (TanStack) |
| **Virtual Scrolling** | Built-in | Optional @tanstack/react-virtual |
| **Cell Editing** | Complex config | Native inline editing |

---

## Part 2: Detailed Implementation Spec

### 2.1 Component Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                     EnterpriseDataTable                          │
│                    (Main Container Component)                    │
├─────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────────────┐│
│  │                      DataTableToolbar                        ││
│  │  ┌──────────┐ ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐  ││
│  │  │ Global   │ │Filter│ │Group │ │Density│ │Export│ │Columns│  ││
│  │  │ Search   │ │Toggle│ │ By   │ │Toggle │ │     │ │       │  ││
│  │  └──────────┘ └──────┘ └──────┘ └──────┘ └──────┘ └──────┘  ││
│  └─────────────────────────────────────────────────────────────┘│
├─────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────────────┐│
│  │                        DataTable                            ││
│  │  ┌─────────────────────────────────────────────────────────┐││
│  │  │  [Actions][Name|][Email][Phone][Status][Total][Options] │││
│  │  │   ┃pin┃  ┃resize┃                                     ┃pin┃ │││
│  │  │   sticky  drag→                                       sticky│││
│  │  └─────────────────────────────────────────────────────────┘││
│  │  ┌─────────────────────────────────────────────────────────┐││
│  │  │  ┌─ Group: Active (12 items) ─┐ ▼                     │││
│  │  │  │   Name    │ Email    │ Amount   │ Count           │││
│  │  │  │   Total:  │          │ $15,000  │ 12              │││
│  │  │  └─────────────────────────────┘                      │││
│  │  │  ┌─ Group: Pending (8 items) ─┐ ▼                     │││
│  │  │  └─────────────────────────────┘                      │││
│  │  └─────────────────────────────────────────────────────────┘││
│  └─────────────────────────────────────────────────────────────┘│
├─────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────────────┐│
│  │                    DataTablePagination                       ││
│  │  [Previous] [1] [2] [3] ... [10] [Next]  [20/page ▼]        ││
│  └─────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                    ColumnsDropdown (Source)                     │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │  Visible Columns                                     ━━━━ │ │
│  │  ☑ ⋮⋮ Name              [Asc ▼]  [📌pin]  [=resize]       │ │
│  │  ☑ ⋮⋮ Email             [Off ▼]  [📌pin]  [=resize]       │ │
│  │  ☐ ⋮⋮ Phone             [---  ]  [  ]     [=resize]       │ │
│  │  ☑ ⋮⋮ Status            [Desc▼]  [  ]     [=resize]       │ │
│  ├────────────────────────────────────────────────────────────┤ │
│  │  Group By: [Status ▼]                                       │ │
│  │  Aggregations: Sum: [Amount ☑] Avg: [Age ☐]                │ │
│  ├────────────────────────────────────────────────────────────┤ │
│  │  [Save View...] [Reset to Default ↺]                        │ │
│  └────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

### 2.2 Hook Specification

```typescript
// hooks/useEnterpriseTable.ts

interface UseEnterpriseTableOptions<TData> {
  // Required
  data: TData[]
  columns: ColumnDef<TData, unknown>[]
  tableKey: string  // For persistence

  // Feature toggles
  enablePinning?: boolean           // default: true
  enableResizing?: boolean         // default: true
  enableGrouping?: boolean         // default: true
  enableSelection?: boolean        // default: true
  enableRangeSelection?: boolean   // default: false
  enableClipboard?: boolean        // default: false
  enableUndoRedo?: boolean         // default: false
  enableVirtualization?: boolean   // default: false

  // Server-side options
  manualPagination?: boolean       // default: true (server-side)
  manualSorting?: boolean          // default: true
  manualFiltering?: boolean        // default: true
  rowCount?: number                // Total for server-side

  // Callbacks
  onRowSelectionChange?: (rows: TData[]) => void
  onRangeSelectionChange?: (range: CellRange | null) => void
  onClipboardCopy?: (cells: CellData[]) => void
  onClipboardPaste?: (cells: CellData[]) => void
}

interface UseEnterpriseTableReturn<TData> {
  // TanStack table instance
  table: Table<TData>

  // Settings (persisted)
  settings: EnterpriseTableSettings
  updateSettings: (updater: SettingsUpdater) => void
  resetToDefault: () => void

  // Feature state
  pinning: ColumnPinningState
  resizing: ColumnSizingState
  grouping: GroupingState
  selection: RowSelectionState
  rangeSelection: RangeSelectionState

  // Actions
  toggleColumnPin: (columnId: string, side: 'left' | 'right' | false) => void
  toggleColumnVisibility: (columnId: string, visible: boolean) => void
  reorderColumns: (oldIndex: number, newIndex: number) => void
  setGrouping: (columnIds: string[]) => void
  expandGroup: (groupId: string) => void
  collapseGroup: (groupId: string) => void

  // Range selection (Excel-like)
  startRangeSelection: (cell: CellPosition) => void
  updateRangeSelection: (cell: CellPosition) => void
  endRangeSelection: () => void
  clearRangeSelection: () => void

  // Clipboard
  copySelection: () => void
  pasteSelection: () => void

  // Undo/Redo
  undo: () => void
  redo: () => void
  canUndo: boolean
  canRedo: boolean

  // Export
  exportToExcel: (options?: ExportOptions) => void
  exportToCSV: (options?: ExportOptions) => void
}

// Usage
const { table, settings, exportToExcel, undo, redo } = useEnterpriseTable({
  data: users,
  columns: userColumns,
  tableKey: 'users',
  enableRangeSelection: true,
  enableClipboard: true,
  enableUndoRedo: true,
})
```

### 2.3 State Management

```typescript
// State shape (persisted to localStorage)
interface EnterpriseTableSettings {
  version: 3  // Schema version for migrations

  // Column configuration
  columnVisibility: Record<string, boolean>
  columnOrder: string[]
  columnSizing: Record<string, number>
  columnPinning: {
    left: string[]
    right: string[]
  }

  // Grouping
  grouping: string[]
  expanded: Record<string, boolean>

  // Sorting (ephemeral, not persisted)
  sorting: SortingState

  // Selection (ephemeral)
  rowSelection: RowSelectionState

  // UI
  density: 'compact' | 'normal' | 'comfortable'
  showFiltersRow: boolean
}

// Ephemeral state (not persisted)
interface EphemeralState {
  rangeSelection: RangeSelectionState
  clipboard: ClipboardState
  undoStack: Command[]
  redoStack: Command[]
}
```

### 2.4 Column Definition API

```typescript
// Extended column definition
const columns: EnterpriseColumnDef<User>[] = [
  // Select column
  createSelectColumn<User>(),

  // Actions column (pinned left by default)
  createActionsColumn<User>({
    pinned: 'left',
    enableResizing: false,
  }),

  // Regular column with full configuration
  {
    accessorKey: 'name',
    header: 'Name',
    size: 200,
    minSize: 100,
    maxSize: 400,
    enableResizing: true,
    enablePinning: true,
    enableSorting: true,
    enableGrouping: false,  // Don't allow grouping by name

    // Pinning
    pinned: false,  // or 'left' | 'right'

    // Sorting
    sortDescFirst: false,
    sortingFn: 'text',  // 'text' | 'numeric' | 'datetime' | custom

    // Filtering
    enableColumnFilter: true,
    filterFn: 'includesString',  // built-in or custom

    // Grouping/Aggregation
    aggregationFn: undefined,  // Can't aggregate names

    // Cell editing
    enableEditing: true,
    editComponent: TextEditor,
    validate: (value) => value.length > 0,

    // Styling
    meta: {
      align: 'left',
      headerClassName: 'font-semibold',
      cellClassName: 'text-primary',
    },
  },

  // Numeric column with aggregation
  {
    accessorKey: 'amount',
    header: 'Amount',
    size: 120,
    align: 'right',
    enableGrouping: true,
    aggregationFn: 'sum',
    aggregatedCell: ({ getValue }) => (
      <strong className="text-green-600">
        {formatCurrency(getValue() as number)}
      </strong>
    ),
    cell: ({ getValue }) => formatCurrency(getValue() as number),
  },

  // Status column with grouping
  {
    accessorKey: 'status',
    header: 'Status',
    enableGrouping: true,
    filterFn: 'equals',
    filterComponent: StatusFilter,
    cell: ({ getValue }) => <StatusBadge status={getValue()} />,
  },
]
```

---

## Part 3: Advanced Features Specification

### 3.1 Range Selection (Excel-like) ⭐ Differentiator

```typescript
// Range selection - unique feature
interface RangeSelectionState {
  isActive: boolean
  start: CellPosition | null
  end: CellPosition | null
}

interface CellPosition {
  rowId: string
  columnId: string
}

// Visual rendering
const RangeSelectionOverlay = () => {
  const { rangeSelection } = useEnterpriseTableContext()

  if (!rangeSelection.start || !rangeSelection.end) return null

  const bounds = calculateBounds(rangeSelection.start, rangeSelection.end)

  return (
    <div
      className="absolute bg-primary/10 border border-primary pointer-events-none"
      style={{
        top: bounds.top,
        left: bounds.left,
        width: bounds.width,
        height: bounds.height,
      }}
    />
  )
}

// Usage
const handleMouseDown = (cell: CellPosition) => {
  if (e.shiftKey && lastSelectedCell) {
    // Shift+click extends selection
    updateRangeSelection(cell)
  } else {
    // New selection
    startRangeSelection(cell)
  }
}

const handleMouseMove = (cell: CellPosition) => {
  if (rangeSelection.isActive) {
    updateRangeSelection(cell)
  }
}

const handleMouseUp = () => {
  endRangeSelection()
}
```

**Keyboard shortcuts:**
- `Shift + Arrow Keys` - Extend selection
- `Shift + Click` - Select range
- `Ctrl + Click` - Add to selection
- `Ctrl + A` - Select all
- `Ctrl + C` - Copy selection
- `Ctrl + V` - Paste

### 3.2 Clipboard Operations ⭐ Differentiator

```typescript
// Clipboard support
interface ClipboardState {
  data: ClipboardCell[][]
  mode: 'copy' | 'cut'
  sourceRange: CellRange
}

interface ClipboardCell {
  value: unknown
  columnId: string
  rowId: string
}

const useClipboard = () => {
  const copySelection = async () => {
    const selection = getSelectedCells()
    const tsv = convertToTSV(selection)

    await navigator.clipboard.writeText(tsv)

    // Also store in internal state for paste within app
    setClipboard({
      data: selection,
      mode: 'copy',
      sourceRange: rangeSelection,
    })
  }

  const pasteSelection = async () => {
    // Try internal clipboard first
    if (clipboard.data) {
      pasteInternal(clipboard)
      return
    }

    // Fall back to system clipboard
    const text = await navigator.clipboard.readText()
    const cells = parseTSV(text)
    pasteCells(cells)
  }

  return { copySelection, pasteSelection }
}
```

### 3.3 Undo/Redo System ⭐ Differentiator

```typescript
// Command pattern for undo/redo
interface Command {
  type: 'edit' | 'delete' | 'insert' | 'reorder' | 'resize' | 'pin'
  execute: () => void
  undo: () => void
  redo: () => void
  description: string
}

const useUndoRedo = () => {
  const [undoStack, setUndoStack] = useState<Command[]>([])
  const [redoStack, setRedoStack] = useState<Command[]>([])

  const execute = (command: Command) => {
    command.execute()
    setUndoStack(prev => [...prev, command])
    setRedoStack([]) // Clear redo on new action
  }

  const undo = () => {
    const command = undoStack[undoStack.length - 1]
    if (command) {
      command.undo()
      setUndoStack(prev => prev.slice(0, -1))
      setRedoStack(prev => [...prev, command])
    }
  }

  const redo = () => {
    const command = redoStack[redoStack.length - 1]
    if (command) {
      command.redo()
      setRedoStack(prev => prev.slice(0, -1))
      setUndoStack(prev => [...prev, command])
    }
  }

  return { execute, undo, redo, canUndo: undoStack.length > 0, canRedo: redoStack.length > 0 }
}

// Example command
const createEditCommand = (rowId, columnId, oldValue, newValue): Command => ({
  type: 'edit',
  description: `Edit ${columnId}`,
  execute: () => updateCell(rowId, columnId, newValue),
  undo: () => updateCell(rowId, columnId, oldValue),
  redo: () => updateCell(rowId, columnId, newValue),
})
```

### 3.4 Excel Export

```typescript
// Excel export using xlsx.js
const exportToExcel = async (options: ExportOptions) => {
  const XLSX = await import('xlsx')

  const data = table.getFilteredRowModel().rows.map(row => {
    const obj: Record<string, unknown> = {}
    row.getVisibleCells().forEach(cell => {
      obj[cell.column.id] = cell.getValue()
    })
    return obj
  })

  const ws = XLSX.utils.json_to_sheet(data)
  const wb = XLSX.utils.book_new()
  XLSX.utils.book_append_sheet(wb, ws, 'Data')

  // Styling (requires xlsx-style or similar)
  ws['!cols'] = table.getVisibleLeafColumns().map(col => ({
    wch: col.getSize() / 7, // Approximate character width
  }))

  XLSX.writeFile(wb, `${options.filename || 'export'}.xlsx`)
}
```

---

## Part 4: Performance Architecture

### 4.1 Virtualization (10k+ rows)

```typescript
// Optional virtualization
import { useVirtualizer } from '@tanstack/react-virtual'

const VirtualDataTable = ({ table, data }) => {
  const parentRef = useRef<HTMLDivElement>(null)

  const virtualizer = useVirtualizer({
    count: data.length,
    getScrollElement: () => parentRef.current,
    estimateSize: () => 44, // Row height
    overscan: 5,
    measureElement: (el) => el.getBoundingClientRect().height,
  })

  const virtualRows = virtualizer.getVirtualItems()

  return (
    <div ref={parentRef} className="overflow-auto h-[600px]">
      <div style={{ height: `${virtualizer.getTotalSize()}px` }}>
        <table>
          <tbody>
            {virtualRows.map(virtualRow => {
              const row = table.getRowModel().rows[virtualRow.index]
              return (
                <tr
                  key={row.id}
                  data-index={virtualRow.index}
                  ref={virtualizer.measureElement}
                  style={{ transform: `translateY(${virtualRow.start}px)` }}
                >
                  {/* cells */}
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>
    </div>
  )
}
```

### 4.2 Resize Performance

```typescript
// Resize with CSS-only updates
const useColumnResizeOptimized = (column) => {
  const [size, setSize] = useState(column.getSize())
  const sizeRef = useRef(size)
  const colRef = useRef<HTMLColElement>(null)

  const onResizeStart = () => {
    colRef.current = document.querySelector(`col[data-col="${column.id}"]`)
  }

  const onResize = (delta: number) => {
    const newSize = Math.max(50, sizeRef.current + delta)
    // Direct DOM update - NO React re-render
    if (colRef.current) {
      colRef.current.style.width = `${newSize}px`
    }
  }

  const onResizeEnd = (finalDelta: number) => {
    const finalSize = Math.max(50, sizeRef.current + finalDelta)
    sizeRef.current = finalSize
    setSize(finalSize) // Single React update
    column.resize(finalSize)
  }

  return { size, onResizeStart, onResize, onResizeEnd }
}
```

### 4.3 Debounced Persistence

```typescript
// Debounced localStorage writes
const useDebouncedSettings = (key: string, settings: Settings) => {
  const save = useMemo(
    () => debounce((s: Settings) => {
      localStorage.setItem(`noir:table:${key}:settings`, JSON.stringify(s))
    }, 300),
    [key]
  )

  useEffect(() => {
    save(settings)
  }, [settings, save])
}
```

---

## Part 5: Implementation Phases

### Phase 1: Foundation — COMPLETE (2026-03-13)
**Deliverables:**
- [x] `useEnterpriseTable` hook with persistence (unified hook replacing `useServerTable`)
- [x] Enhanced `DataTable` with pinning support
- [x] Column resizing with `onEnd` mode
- [x] Versioned storage with migration (`enterpriseSettingsStorage.ts`)
- [x] Density toggle (compact/normal/comfortable)
- [x] Drag-to-reorder columns via `DataTableColumnsDropdown`
- [x] All 24 production pages migrated to `useEnterpriseTable`
- [x] Playwright verification: 24/24 pages pass

**Testing:** Verified via automated Playwright audit

### Phase 2: Column Management — COMPLETE (2026-03-13)
**Deliverables:**
- [x] Enhanced Columns Dropdown (`DataTableColumnsDropdown.tsx`)
- [x] Vertical drag-drop reorder (via `@dnd-kit`)
- [x] Sort configuration UI (per-column asc/desc toggle)
- [x] Visibility toggle (checkbox per column)
- [x] Reset to default

**Testing:** Verified via Playwright audit — all dropdown features functional

### Phase 3A: Row Animation
**Deliverables:**
- [ ] CSS keyframes for highlight flash and fade-out
- [ ] DataTable `getRowClassName` prop
- [ ] `useRowHighlight` hook wired into all pages with create/delete mutations
- [ ] Smooth add/remove row UX

**Testing:**
- Create entity → row flashes briefly
- Delete entity → row fades out, then disappears

### Phase 3B: Grouping & Aggregation
**Deliverables:**
- [ ] Row grouping with `getGroupedRowModel`
- [ ] Expandable group headers
- [ ] "Group by" dropdown in DataTableToolbar
- [ ] Aggregation functions (sum, count, avg, min, max)
- [ ] Grouping state persistence in localStorage
- [ ] Server-side grouping support (deferred if not needed)

**Testing:**
- Groups collapse/expand
- Aggregations calculate correctly
- Works with 1000+ rows
- Grouping state persists across page reload

### ~~Phase 4: Advanced Features~~ — REMOVED
> Range selection, clipboard, undo/redo, Excel/CSV export removed. Low ROI for current use cases. Specs preserved in Part 3 of this document for future reference.

### Phase 5: Polish & Performance
**Deliverables:**
- [ ] Virtual scrolling (optional, for pages with 100+ rows)
- [ ] Keyboard navigation (arrow keys, Enter, Space, Home/End)
- [ ] Mobile touch improvements
- [ ] Storybook documentation updates
- [ ] E2E test suite for DataTable features

**Testing:**
- 500 rows with virtualization
- All keyboard shortcuts functional
- Mobile responsive

---

## Part 6: Component API Reference

### DataTable

```typescript
interface DataTableProps<TData> {
  table: Table<TData>

  // Rendering
  isLoading?: boolean
  emptyState?: React.ReactNode
  skeletonRowCount?: number

  // Features
  enableRangeSelection?: boolean
  enableClipboard?: boolean
  enableVirtualization?: boolean

  // Callbacks
  onRowClick?: (row: TData) => void
  onCellClick?: (cell: Cell<TData>) => void
  onRangeSelectionChange?: (range: CellRange | null) => void

  // Styling
  className?: string
  density?: 'compact' | 'normal' | 'comfortable'
}
```

### DataTableToolbar

```typescript
interface DataTableToolbarProps<TData> {
  table: Table<TData>

  // Search
  searchInput?: string
  onSearchChange?: (value: string) => void

  // Filters
  showFiltersToggle?: boolean
  hasActiveFilters?: boolean
  onResetFilters?: () => void

  // Export
  onExportExcel?: () => void
  onExportCSV?: () => void

  // Density
  density?: 'compact' | 'normal' | 'comfortable'
  onDensityChange?: (density: Density) => void

  // Slots
  filterSlot?: React.ReactNode
  actionSlot?: React.ReactNode
}
```

### ColumnsDropdown

```typescript
interface ColumnsDropdownProps<TData> {
  table: Table<TData>
  settings: EnterpriseTableSettings

  // Feature toggles
  enableReorder?: boolean
  enableSortConfig?: boolean
  enablePinConfig?: boolean
  enableGrouping?: boolean

  // Actions
  onVisibilityChange: (columnId: string, visible: boolean) => void
  onReorder: (oldIndex: number, newIndex: number) => void
  onSortChange: (columnId: string, direction: SortDirection) => void
  onPinChange: (columnId: string, side: PinSide) => void
  onGroupingChange: (columnIds: string[]) => void
  onReset: () => void
}
```

---

## Part 7: Success Metrics

### Feature Parity

| Feature | Target | AG Grid | Status |
|---------|--------|---------|--------|
| Pinning | ✅ | ✅ | Match |
| Resizing | ✅ | ✅ | Match |
| Reorder | ✅ | ✅ | Match |
| Grouping | ✅ | ✅ | Match |
| Aggregation | ✅ | ✅ | Match |
| Range Selection | ✅ | ❌ | **Exceed** |
| Clipboard | ✅ | ⚠️ | Match/Exceed |
| Undo/Redo | ✅ | ⚠️ | Match/Exceed |
| Excel Export | ✅ | ✅ | Match |

### Performance Benchmarks

| Metric | Target | AG Grid |
|--------|--------|---------|
| Bundle Size | <200KB | ~800KB |
| Initial Render | <100ms | ~200ms |
| 1k rows | <500ms | <500ms |
| 10k rows (virtualized) | <1s | <1s |
| Resize FPS | 60 | 60 |
| Sort 1k rows | <100ms | <100ms |

### Accessibility

- [ ] WCAG 2.1 AA compliant
- [ ] Full keyboard navigation
- [ ] Screen reader optimized
- [ ] Touch device support

---

## Part 8: Conclusion

This specification provides a **complete roadmap** to build an enterprise data table that:

1. **Matches AG Grid Enterprise** core features
2. **Exceeds** in range selection, bundle size, and DX
3. **Integrates seamlessly** with existing NOIR architecture
4. **Scales** from simple to complex use cases

**Current Status**: Phases 1-3 + 5 complete (column management, drag-reorder, density, pinning, resizing, row highlighting, grouping, aggregation, virtualization, keyboard navigation). Phase 4 (range selection, clipboard, Excel export) was de-scoped as low ROI. Live across all list pages via `useEnterpriseTable` — see `.claude/rules/datatable-standard.md`.
