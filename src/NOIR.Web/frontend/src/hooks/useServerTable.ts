import { useMemo } from 'react'
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
}: UseServerTableOptions<TData>) => {
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
      columnVisibility: state.columnVisibility ?? {},
      columnOrder: state.columnOrder ?? [],
    },

    onPaginationChange,
    onSortingChange,
    onRowSelectionChange,
    onColumnVisibilityChange,
    onColumnOrderChange,

    meta: meta as TableOptions<TData>['meta'],
  })

  return table
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
