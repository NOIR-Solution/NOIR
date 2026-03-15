import type {
  SortingState,
  ColumnPinningState,
  RowSelectionState,
  GroupingState,
  ColumnDef,
  Table,
  OnChangeFn,
  TableOptions,
} from '@tanstack/react-table'

/**
 * Enterprise DataTable Settings - persisted to localStorage
 */
export interface EnterpriseTableSettings {
  /** Schema version for migrations */
  version: number

  /** Column visibility map */
  columnVisibility: Record<string, boolean>

  /** Column order (array of column IDs) */
  columnOrder: string[]

  /** Column sizes (column ID -> width in pixels) */
  columnSizing: Record<string, number>

  /** Pinned columns */
  columnPinning: {
    left: string[]
    right: string[]
  }

  /** Grouping configuration (array of column IDs to group by) */
  grouping: string[]

  /** Expanded groups — `true` means all expanded (TanStack sentinel), object for individual */
  expanded: true | Record<string, boolean>

  /** UI density */
  density: 'compact' | 'normal' | 'comfortable'

  /** Show filters row */
  showFiltersRow: boolean
}

/**
 * Default settings factory
 */
export const createDefaultSettings = (
  columnIds: string[],
  defaultPinLeft: string[] = ['actions', 'select']
): EnterpriseTableSettings => ({
  version: 3,
  columnVisibility: Object.fromEntries(columnIds.map(id => [id, true])),
  columnOrder: columnIds,
  columnSizing: {},
  columnPinning: {
    left: defaultPinLeft.filter(id => columnIds.includes(id)),
    right: [],
  },
  grouping: [],
  expanded: {},
  density: 'normal',
  showFiltersRow: false,
})

/**
 * Options for useEnterpriseTable hook
 *
 * Unified hook replacing both useServerTable and the original useEnterpriseTable.
 * Server-side state (pagination, sorting) is managed externally via props.
 * Enterprise UI state (visibility, order, sizing, pinning, density) is managed
 * internally and persisted to localStorage.
 */
export interface EnterpriseTableOptions<TData> {
  /** Table data */
  data: TData[]

  /** Column definitions */
  columns: ColumnDef<TData, unknown>[]

  /** Unique key for localStorage persistence (e.g. 'users', 'orders') */
  tableKey: string

  /** Total row count (for server-side pagination pageCount calculation) */
  rowCount?: number

  /** Default pinned columns on left */
  defaultPinLeft?: string[]

  // ─── Feature toggles ────────────────────────────────────────────────────────

  enablePinning?: boolean
  enableResizing?: boolean
  enableGrouping?: boolean
  /** Enable row selection — boolean or per-row function */
  enableRowSelection?: boolean | ((row: { original: TData }) => boolean)

  // ─── Server-side flags (all default to true) ────────────────────────────────

  manualPagination?: boolean
  manualSorting?: boolean
  manualFiltering?: boolean

  // ─── External server-side state ─────────────────────────────────────────────

  /** External state from URL params (useTableParams) */
  state?: {
    pagination?: { pageIndex: number; pageSize: number }
    sorting?: SortingState
  }

  /** Called when pagination changes (from DataTablePagination) */
  onPaginationChange?: OnChangeFn<{ pageIndex: number; pageSize: number }>

  /** Called when sorting changes (from column header clicks) */
  onSortingChange?: OnChangeFn<SortingState>

  // ─── Table config ───────────────────────────────────────────────────────────

  /** Stable row ID getter for selection across pages */
  getRowId?: TableOptions<TData>['getRowId']

  /** Allow removing sort direction (default: false for admin tables) */
  enableSortingRemoval?: boolean

  /** Prevent page reset on data changes (default: false) */
  autoResetPageIndex?: boolean

  /** Arbitrary table meta */
  meta?: Record<string, unknown>

  // ─── Callbacks ──────────────────────────────────────────────────────────────

  /** Called when row selection changes with resolved row data */
  onRowSelectionChange?: (selectedRows: TData[]) => void
}

/**
 * Return type for useEnterpriseTable hook
 */
export interface EnterpriseTableReturn<TData> {
  /** TanStack table instance */
  table: Table<TData>

  /** Current persisted settings (visibility, order, sizing, pinning, density) */
  settings: EnterpriseTableSettings

  /** Update settings (persists automatically) */
  updateSettings: (updater: Partial<EnterpriseTableSettings> | ((prev: EnterpriseTableSettings) => EnterpriseTableSettings)) => void

  /** Reset all settings to default */
  resetToDefault: () => void

  /** Check if any customization exists */
  isCustomized: boolean

  /** Column actions */
  toggleColumnVisibility: (columnId: string, visible: boolean) => void
  reorderColumns: (oldIndex: number, newIndex: number) => void
  toggleColumnPin: (columnId: string, side: 'left' | 'right' | false) => void

  /** Grouping actions */
  setGrouping: (columnIds: string[]) => void
  toggleGroupExpansion: (groupId: string) => void
  expandAllGroups: () => void
  collapseAllGroups: () => void

  /** UI actions */
  setDensity: (density: EnterpriseTableSettings['density']) => void
  toggleFiltersRow: () => void
}

// Re-export TanStack types for convenience
export type { ColumnDef, Table, SortingState, ColumnPinningState, RowSelectionState, GroupingState, OnChangeFn }
