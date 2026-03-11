import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import {
  useReactTable,
  getCoreRowModel,
  type ColumnDef,
  type SortingState,
  type RowSelectionState,
  type OnChangeFn,
  type TableOptions,
  type RowData,
} from '@tanstack/react-table'

type ColumnVisibilityState = Record<string, boolean>
type ColumnOrderState = string[]

const STORAGE_PREFIX = 'noir:col-vis:'

const readStorage = (key: string): ColumnVisibilityState => {
  try {
    const raw = localStorage.getItem(STORAGE_PREFIX + key)
    return raw ? JSON.parse(raw) : {}
  } catch {
    return {}
  }
}

const writeStorage = (key: string, state: ColumnVisibilityState) => {
  try {
    const hasHidden = Object.values(state).some((v) => v === false)
    if (hasHidden) {
      localStorage.setItem(STORAGE_PREFIX + key, JSON.stringify(state))
    } else {
      localStorage.removeItem(STORAGE_PREFIX + key)
    }
  } catch { /* quota exceeded — silently ignore */ }
}

interface UseServerTableOptions<TData extends RowData> {
  data: TData[]
  columns: ColumnDef<TData, unknown>[]
  /** Total rows across all pages (used to calculate pageCount) */
  rowCount: number
  state: {
    pagination: { pageIndex: number; pageSize: number }
    sorting?: SortingState
    rowSelection?: RowSelectionState
    columnVisibility?: ColumnVisibilityState
    columnOrder?: ColumnOrderState
  }
  onPaginationChange: OnChangeFn<{ pageIndex: number; pageSize: number }>
  onSortingChange?: OnChangeFn<SortingState>
  onRowSelectionChange?: OnChangeFn<RowSelectionState>
  onColumnVisibilityChange?: OnChangeFn<ColumnVisibilityState>
  onColumnOrderChange?: OnChangeFn<ColumnOrderState>
  enableRowSelection?: boolean | ((row: { original: TData }) => boolean)
  /** Must provide getRowId for stable selection across pages */
  getRowId?: TableOptions<TData>['getRowId']
  meta?: Record<string, unknown>
  /** localStorage key for persisting column visibility (e.g. 'customers', 'orders') */
  columnVisibilityStorageKey?: string
}

/**
 * Thin wrapper around useReactTable with server-side defaults pre-configured:
 * - manualPagination / manualSorting / manualFiltering: true
 * - autoResetPageIndex: false  (we control page resets)
 * - enableSortingRemoval: false (admin tables always have a sort direction)
 * - getCoreRowModel only (no client-side row models — server handles everything)
 */
export const useServerTable = <TData extends RowData>({
  data,
  columns,
  rowCount,
  state,
  onPaginationChange,
  onSortingChange,
  onRowSelectionChange,
  onColumnVisibilityChange,
  onColumnOrderChange,
  enableRowSelection = false,
  getRowId,
  meta,
  columnVisibilityStorageKey,
}: UseServerTableOptions<TData>) => {
  // Manage column visibility internally when caller doesn't provide it
  const [internalColumnVisibility, setInternalColumnVisibility] = useState<ColumnVisibilityState>(
    () => (columnVisibilityStorageKey ? readStorage(columnVisibilityStorageKey) : {}),
  )
  const resolvedColumnVisibility = state.columnVisibility ?? internalColumnVisibility
  const resolvedOnColumnVisibilityChange = onColumnVisibilityChange ?? setInternalColumnVisibility

  // Persist to localStorage when internal state changes
  const storageKeyRef = useRef(columnVisibilityStorageKey)
  storageKeyRef.current = columnVisibilityStorageKey
  useEffect(() => {
    if (storageKeyRef.current && !onColumnVisibilityChange) {
      writeStorage(storageKeyRef.current, internalColumnVisibility)
    }
  }, [internalColumnVisibility, onColumnVisibilityChange])

  const resetColumnVisibility = useCallback(() => {
    setInternalColumnVisibility({})
    if (columnVisibilityStorageKey) {
      localStorage.removeItem(STORAGE_PREFIX + columnVisibilityStorageKey)
    }
  }, [columnVisibilityStorageKey])

  const table = useReactTable({
    data,
    columns,
    getCoreRowModel: getCoreRowModel(),

    // Server-side: all row models are manual
    manualPagination: true,
    manualSorting: true,
    manualFiltering: true,

    // Calculate pageCount from rowCount (not hardcoded)
    rowCount,

    // Prevent TanStack Table from resetting page when data changes
    autoResetPageIndex: false,

    // Admin tables always have a sort direction — no "unsorted" state
    enableSortingRemoval: false,

    // Stable selection across pages — require explicit getRowId
    getRowId,
    enableRowSelection,

    state: {
      pagination: state.pagination,
      sorting: state.sorting ?? [],
      rowSelection: state.rowSelection ?? {},
      columnVisibility: resolvedColumnVisibility,
      columnOrder: state.columnOrder ?? [],
    },

    onPaginationChange,
    onSortingChange,
    onRowSelectionChange,
    onColumnVisibilityChange: resolvedOnColumnVisibilityChange,
    onColumnOrderChange,

    meta: meta as TableOptions<TData>['meta'],
  })

  return useMemo(() => Object.assign(table, { resetColumnVisibility }), [table, resetColumnVisibility])
}

/**
 * Derive selected row IDs from raw rowSelection state (performance-correct pattern).
 * Do NOT call table.getFilteredSelectedRowModel() for bulk action counts —
 * it recalculates the full row model on every render.
 */
export const getSelectedIds = (rowSelection: RowSelectionState): string[] =>
  Object.keys(rowSelection)

/**
 * useMemo-wrapped helper used in page components:
 *   const selectedIds = useSelectedIds(table.getState().rowSelection)
 */
export const useSelectedIds = (rowSelection: RowSelectionState): string[] =>
  useMemo(() => Object.keys(rowSelection), [rowSelection])
