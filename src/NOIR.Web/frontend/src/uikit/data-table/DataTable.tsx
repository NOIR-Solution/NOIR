import { flexRender, type Table, type RowData } from '@tanstack/react-table'
import { cn } from '@/lib/utils'
import {
  Table as UITable,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '../table/Table'
import { Skeleton } from '../skeleton/Skeleton'
import { EmptyState } from '../empty-state/EmptyState'

interface DataTableProps<TData extends RowData> {
  table: Table<TData>
  /** Show loading skeletons */
  isLoading?: boolean
  /**
   * Show stale/pending visual (opacity dimming).
   * Pass `isPlaceholderData || isFilterPending || isSearchStale`.
   */
  isStale?: boolean
  /** Custom empty state — defaults to built-in EmptyState */
  emptyState?: React.ReactNode
  /** Called when user clicks a non-action row cell */
  onRowClick?: (row: TData) => void
  /**
   * Number of skeleton rows to show during loading.
   * Defaults to 5.
   */
  skeletonRowCount?: number
  className?: string
}

/**
 * Headless DataTable renderer that composes the existing UIKit Table primitives.
 * Integrates with useServerTable + useTableParams hooks.
 *
 * Performance notes:
 * - Column resizing uses CSS variable technique (zero React re-renders during drag)
 * - Row selection reads from table.getState().rowSelection — don't call getFilteredSelectedRowModel()
 *   for bulk action counts (use useSelectedIds hook from useServerTable instead)
 */
export const DataTable = <TData extends RowData>({
  table,
  isLoading = false,
  isStale = false,
  emptyState,
  onRowClick,
  skeletonRowCount = 5,
  className,
}: DataTableProps<TData>) => {
  const columns = table.getAllColumns()
  const visibleColumnCount = table.getVisibleLeafColumns().length

  // CSS variable technique for column sizing — updates vars only, no React re-renders during resize
  const columnSizeVars = Object.fromEntries(
    columns.map((col) => [`--col-${col.id}-size`, `${col.getSize()}px`]),
  ) as React.CSSProperties

  return (
    <div
      className={cn(
        'transition-opacity duration-150',
        isStale && 'opacity-60 pointer-events-none',
        className,
      )}
    >
      <UITable style={columnSizeVars}>
        <TableHeader>
          {table.getHeaderGroups().map((headerGroup) => (
            <TableRow key={headerGroup.id} className="hover:bg-transparent">
              {headerGroup.headers.map((header) => (
                <TableHead
                  key={header.id}
                  colSpan={header.colSpan}
                  className={cn(header.column.columnDef.meta?.headerClassName)}
                  style={{ width: `var(--col-${header.column.id}-size)` }}
                >
                  {header.isPlaceholder
                    ? null
                    : flexRender(header.column.columnDef.header, header.getContext())}
                </TableHead>
              ))}
            </TableRow>
          ))}
        </TableHeader>

        <TableBody>
          {isLoading ? (
            // Skeleton rows
            Array.from({ length: skeletonRowCount }).map((_, i) => (
              <TableRow key={`skeleton-${i}`} className="hover:bg-transparent">
                {Array.from({ length: visibleColumnCount }).map((_, j) => (
                  <TableCell key={j}>
                    <Skeleton className="h-4 w-full" />
                  </TableCell>
                ))}
              </TableRow>
            ))
          ) : table.getRowModel().rows.length === 0 ? (
            // Empty state
            <TableRow className="hover:bg-transparent">
              <TableCell colSpan={visibleColumnCount} className="py-12 text-center">
                {emptyState ?? (
                  <EmptyState
                    title="No results"
                    description="Try adjusting your search or filters."
                  />
                )}
              </TableCell>
            </TableRow>
          ) : (
            // Data rows
            table.getRowModel().rows.map((row) => (
              <TableRow
                key={row.id}
                data-state={row.getIsSelected() ? 'selected' : undefined}
                onClick={
                  onRowClick
                    ? (e) => {
                        // Don't trigger row click when clicking interactive elements (checkboxes, buttons, links)
                        const target = e.target as HTMLElement
                        if (
                          target.closest('button, a, input, [role="checkbox"], [role="menuitem"]')
                        ) {
                          return
                        }
                        onRowClick(row.original)
                      }
                    : undefined
                }
                className={cn(onRowClick && 'cursor-pointer')}
              >
                {row.getVisibleCells().map((cell) => (
                  <TableCell
                    key={cell.id}
                    className={cn(
                      cell.column.columnDef.meta?.cellClassName,
                      cell.column.columnDef.meta?.align === 'center' && 'text-center',
                      cell.column.columnDef.meta?.align === 'right' && 'text-right',
                    )}
                    style={{ width: `var(--col-${cell.column.id}-size)` }}
                  >
                    {flexRender(cell.column.columnDef.cell, cell.getContext())}
                  </TableCell>
                ))}
              </TableRow>
            ))
          )}
        </TableBody>
      </UITable>
    </div>
  )
}

export type { DataTableProps }
