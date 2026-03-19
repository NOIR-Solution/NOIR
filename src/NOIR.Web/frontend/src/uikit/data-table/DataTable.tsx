import { useCallback, useState } from 'react'
import {
  flexRender,
  type Table,
  type RowData,
  type Header,
} from '@tanstack/react-table'
import {
  DndContext,
  DragOverlay,
  closestCenter,
  PointerSensor,
  KeyboardSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
  type DragStartEvent,
} from '@dnd-kit/core'
import {
  SortableContext,
  horizontalListSortingStrategy,
  useSortable,
  arrayMove,
  sortableKeyboardCoordinates,
} from '@dnd-kit/sortable'
import { CSS } from '@dnd-kit/utilities'
import { ChevronDown, ChevronRight, GripVertical } from 'lucide-react'
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
import { DataTableHeaderContextMenu } from './DataTableHeaderContextMenu'

// ─── Internal: per-header draggable TH ───────────────────────────────────────

// eslint-disable-next-line @typescript-eslint/no-explicit-any
const DraggableTableHead = ({ header, canDragReorder }: {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  header: Header<any, unknown>
  canDragReorder: boolean
}) => {
  const isMetaSticky = header.column.columnDef.meta?.sticky === 'left'
  const nativePinLeft = header.column.getIsPinned() === 'left'
  const nativePinRight = header.column.getIsPinned() === 'right'
  const minSize = header.column.columnDef.minSize
  const maxSize = header.column.columnDef.maxSize
  const isFixed = minSize !== undefined && maxSize !== undefined && minSize === maxSize
  const isPinned = isMetaSticky || nativePinLeft || nativePinRight
  const isDraggable = canDragReorder && !isFixed && !isPinned

  const { attributes, listeners, setNodeRef, setActivatorNodeRef, transform, transition, isDragging } = useSortable({
    id: header.column.id,
    disabled: !isDraggable,
  })

  const sortableStyle: React.CSSProperties = isDraggable
    ? {
        transform: CSS.Transform.toString(transform),
        transition,
      }
    : {}

  const tableHead = (
    <TableHead
      ref={isDraggable ? setNodeRef : undefined}
      colSpan={header.colSpan}
      className={cn(
        'group relative',
        header.column.columnDef.meta?.headerClassName,
        header.column.columnDef.meta?.align === 'center' && 'text-center',
        isMetaSticky && 'sticky left-0 z-10 bg-background',
        nativePinLeft &&
          !isMetaSticky &&
          'sticky z-10 bg-background shadow-[2px_0_4px_-2px_hsl(var(--border))]',
        nativePinRight &&
          'sticky z-10 bg-background shadow-[-2px_0_4px_-2px_hsl(var(--border))]',
        isFixed && 'w-[var(--col-fixed-size)]',
        isDragging && 'bg-muted/50 opacity-40 outline-2 outline-dashed outline-primary/30',
      )}
      style={{
        ...sortableStyle,
        width: isFixed ? `${minSize}px` : `var(--col-${header.column.id}-size)`,
        minWidth: isFixed ? `${minSize}px` : `var(--col-${header.column.id}-min-size, auto)`,
        maxWidth: isFixed ? `${maxSize}px` : `var(--col-${header.column.id}-max-size, auto)`,
        ...(isFixed && ({ '--col-fixed-size': `${minSize}px` } as React.CSSProperties)),
        // Centered headers: zero th padding — inner flex div handles centering symmetrically
        ...(header.column.columnDef.meta?.align === 'center' && { padding: 0 }),
        ...(nativePinLeft && !isMetaSticky && { left: `${header.column.getStart('left')}px` }),
        ...(nativePinRight && { right: `${header.column.getAfter('right')}px` }),
      }}
    >
      {/* Drag handle — visible on hover for reorderable columns */}
      {isDraggable && (
        <button
          ref={setActivatorNodeRef}
          type="button"
          {...attributes}
          {...listeners}
          className={cn(
            'absolute left-0.5 top-1/2 -translate-y-1/2 cursor-grab touch-none transition-opacity',
            isDragging
              ? 'opacity-0'
              : 'opacity-0 group-hover:opacity-40 hover:!opacity-80 active:cursor-grabbing',
          )}
          aria-label="Drag to reorder column"
        >
          <GripVertical className="h-3.5 w-3.5" />
        </button>
      )}

      {/* Header content — indent when drag handle present */}
      <div className={cn(isDraggable && 'pl-4')}>
        {header.isPlaceholder
          ? null
          : header.column.columnDef.meta?.align === 'center'
            ? (
              <div className="flex h-full w-full items-center justify-center">
                {flexRender(header.column.columnDef.header, header.getContext())}
              </div>
            )
            : flexRender(header.column.columnDef.header, header.getContext())}
      </div>

      {/* Resize handle — on right edge, shown on hover */}
      {!header.isPlaceholder && header.column.getCanResize() && (
        <div
          role="separator"
          aria-orientation="vertical"
          aria-label="Resize column"
          onMouseDown={header.getResizeHandler()}
          onTouchStart={header.getResizeHandler()}
          className={cn(
            'absolute right-0 top-0 h-full w-1 cursor-col-resize touch-none select-none',
            'bg-transparent opacity-0 group-hover:opacity-100',
            'hover:bg-border/80',
            header.column.getIsResizing() && 'opacity-100 bg-primary/50',
          )}
        />
      )}
    </TableHead>
  )

  return (
    <DataTableHeaderContextMenu column={header.column}>
      {tableHead}
    </DataTableHeaderContextMenu>
  )
}

// ─── Main DataTable component ─────────────────────────────────────────────────

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
  /**
   * Returns a CSS class name for row animation (highlight flash, fade-out).
   * Use with `useRowHighlight` hook.
   */
  getRowAnimationClass?: (rowId: string) => string
  /**
   * Enable keyboard navigation (Arrow keys, Enter, Space, Home/End, Escape).
   * Defaults to true.
   */
  enableKeyboardNav?: boolean
  /**
   * Index of the currently focused row (from useKeyboardNavigation).
   * When provided, the focused row gets a ring highlight.
   */
  focusedRowIndex?: number | null
  /**
   * Props to spread on the table body wrapper for keyboard navigation.
   * From useKeyboardNavigation().tableBodyProps.
   */
  keyboardNavProps?: Record<string, unknown>
}

/**
 * Headless DataTable renderer that composes the existing UIKit Table primitives.
 * Integrates with useEnterpriseTable + useTableParams hooks.
 *
 * Performance notes:
 * - Column resizing uses CSS variable technique (zero React re-renders during drag)
 * - Row selection reads from table.getState().rowSelection — don't call getFilteredSelectedRowModel()
 *   for bulk action counts (use useSelectedIds hook from useEnterpriseTable instead)
 *
 * Enterprise features (when useEnterpriseTable is used):
 * - Column pinning: supports both meta.sticky and TanStack native columnPinning state
 * - Column resizing: resize handle on right edge of each resizable header (drag to resize)
 * - Column reorder: drag grip icon in each header (requires columnOrder state to be set)
 * - Row grouping: expandable group rows with per-column aggregate values
 */
export const DataTable = <TData extends RowData>({
  table,
  isLoading = false,
  isStale = false,
  emptyState,
  onRowClick,
  skeletonRowCount = 5,
  className,
  getRowAnimationClass,
  focusedRowIndex = null,
  keyboardNavProps,
}: DataTableProps<TData>) => {
  const visibleColumns = table.getVisibleLeafColumns()
  const visibleColumnCount = visibleColumns.length

  // Column ordering enabled when explicit columnOrder state is set (useEnterpriseTable)
  const columnOrder = table.getState().columnOrder
  const canDragReorder = columnOrder.length > 0

  // Active drag state for DragOverlay
  const [activeId, setActiveId] = useState<string | null>(null)

  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 5 } }),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates }),
  )

  const handleHeaderDragStart = useCallback((event: DragStartEvent) => {
    setActiveId(String(event.active.id))
  }, [])

  const handleHeaderDragEnd = useCallback(
    (event: DragEndEvent) => {
      setActiveId(null)
      const { active, over } = event
      if (!over || active.id === over.id) return
      const currentOrder = table.getState().columnOrder
      const oldIndex = currentOrder.indexOf(String(active.id))
      const newIndex = currentOrder.indexOf(String(over.id))
      if (oldIndex === -1 || newIndex === -1) return
      table.setColumnOrder(arrayMove(currentOrder, oldIndex, newIndex))
    },
    [table],
  )

  // Resolve the label for the active drag column (for DragOverlay)
  const activeColumn = activeId ? table.getColumn(activeId) : null
  const activeLabel = activeColumn
    ? (activeColumn.columnDef.meta?.label ??
       (typeof activeColumn.columnDef.header === 'string' ? activeColumn.columnDef.header : activeId))
    : null

  // CSS variable technique for column sizing — use col.getSize() to pick up live resize state
  const columnSizeVars = Object.fromEntries(
    table.getAllColumns().map((col) => {
      const minSize = col.columnDef.minSize
      const maxSize = col.columnDef.maxSize
      const isFixed = minSize !== undefined && maxSize !== undefined && minSize === maxSize
      return [
        `--col-${col.id}-size`,
        isFixed ? `${minSize}px` : `${col.getSize()}px`,
      ]
    }),
  ) as React.CSSProperties

  const hasRows = table.getRowModel().rows.length > 0

  // Minimum table width to force horizontal scroll before columns shrink
  const minTableWidth = table.getAllColumns().reduce((acc, col) => {
    return acc + (col.columnDef.minSize ?? col.columnDef.size ?? 100)
  }, 0)

  return (
    <DndContext
      sensors={sensors}
      collisionDetection={closestCenter}
      onDragStart={handleHeaderDragStart}
      onDragEnd={handleHeaderDragEnd}
    >
      <div
        className={cn(
          'rounded-xl border border-border/50 overflow-hidden transition-opacity duration-300 [container-type:inline-size]',
          isStale && 'opacity-60 pointer-events-none',
          className,
        )}
        {...(keyboardNavProps as React.HTMLAttributes<HTMLDivElement>)}
      >
        <UITable
          style={{ ...columnSizeVars, ...((isLoading || hasRows) && { minWidth: `${minTableWidth}px` }) }}
          wrapperClassName={(!isLoading && !hasRows) ? 'overflow-x-hidden' : undefined}
        >
          {/* Colgroup — fixed cols (minSize===maxSize) get exact px widths,
              flex cols have no width (table-auto lets the browser size them to content).
              Skip when empty to avoid subpixel scrollbar artifacts. */}
          {(isLoading || hasRows) && (
          <colgroup>
            {visibleColumns.map((column) => {
              const minSize = column.columnDef.minSize
              const maxSize = column.columnDef.maxSize
              const isFixed = minSize !== undefined && maxSize !== undefined && minSize === maxSize
              return (
                <col
                  key={column.id}
                  data-col-id={column.id}
                  style={isFixed ? { width: `${minSize}px` } : undefined}
                />
              )
            })}
          </colgroup>
          )}

          {/* Header — hidden when empty (no data to contextualize, avoids truncated headers) */}
          {(isLoading || hasRows) && (
          <TableHeader>
            {table.getHeaderGroups().map((headerGroup) => (
              <TableRow key={headerGroup.id} className="hover:bg-transparent">
                <SortableContext
                  items={canDragReorder ? columnOrder : []}
                  strategy={horizontalListSortingStrategy}
                >
                  {headerGroup.headers.map((header) =>
                    // Skip headers absorbed by colSpan (TanStack sets colSpan=0 when grouping)
                    header.colSpan === 0 ? null : (
                      <DraggableTableHead
                        key={header.id}
                        header={header}
                        canDragReorder={canDragReorder}
                      />
                    ),
                  )}
                </SortableContext>
              </TableRow>
            ))}
          </TableHeader>
          )}

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
            // Data rows — includes group rows and aggregate rows when grouping is active
            table.getRowModel().rows.map((row, rowIndex) => {
              // ── Group header row ──────────────────────────────────────────
              if (row.getIsGrouped()) {
                // colSpan approach: label spans from col 0 to the first aggregated column,
                // then remaining cells render individually so aggregated values align with headers.
                const allCells = row.getVisibleCells()
                const firstAggIdx = allCells.findIndex((c) => c.getIsAggregated())
                // If no aggregated cells, span entire row; otherwise span up to first aggregated
                const labelSpan = firstAggIdx === -1 ? allCells.length : firstAggIdx

                return (
                  <TableRow key={row.id} className="bg-muted/40 hover:bg-muted/60">
                    {/* Group label — spans from start to first aggregated column */}
                    <TableCell colSpan={labelSpan} className="py-2 pl-3">
                      <button
                        type="button"
                        onClick={row.getToggleExpandedHandler()}
                        className="flex cursor-pointer items-center gap-2 text-sm font-medium"
                        aria-expanded={row.getIsExpanded()}
                      >
                        {row.getIsExpanded() ? (
                          <ChevronDown className="h-4 w-4 shrink-0 text-muted-foreground" />
                        ) : (
                          <ChevronRight className="h-4 w-4 shrink-0 text-muted-foreground" />
                        )}
                        <span>{String(row.groupingValue ?? '')}</span>
                        <span className="ml-1 text-xs font-normal text-muted-foreground">
                          ({row.subRows.length})
                        </span>
                      </button>
                    </TableCell>
                    {/* Remaining cells after colSpan — aggregated get content, others empty */}
                    {firstAggIdx !== -1 && allCells.slice(firstAggIdx).map((cell) =>
                      cell.getIsAggregated() ? (
                        <TableCell
                          key={cell.id}
                          className={cn(
                            'py-2 text-sm text-muted-foreground',
                            cell.column.columnDef.meta?.align === 'right' && 'text-right',
                            cell.column.columnDef.meta?.align === 'center' && 'text-center',
                          )}
                        >
                          {flexRender(
                            cell.column.columnDef.aggregatedCell ?? cell.column.columnDef.cell,
                            cell.getContext(),
                          )}
                        </TableCell>
                      ) : (
                        <TableCell key={cell.id} />
                      ),
                    )}
                  </TableRow>
                )
              }

              // ── Regular data row ──────────────────────────────────────────
              const isFocused = focusedRowIndex === rowIndex
              return (
                <TableRow
                  key={row.id}
                  id={`table-row-${rowIndex}`}
                  data-row-index={rowIndex}
                  data-state={row.getIsSelected() ? 'selected' : undefined}
                  data-focused={isFocused || undefined}
                  onClick={
                    onRowClick
                      ? (e) => {
                          const target = e.target as HTMLElement
                          if (
                            target.closest(
                              'button, a, input, [role="checkbox"], [role="menuitem"]',
                            )
                          ) {
                            return
                          }
                          onRowClick(row.original)
                        }
                      : undefined
                  }
                  className={cn(
                    onRowClick && 'cursor-pointer',
                    getRowAnimationClass?.(row.id),
                    isFocused && 'ring-2 ring-primary/40 ring-inset',
                  )}
                >
                  {row.getVisibleCells().map((cell) => {
                    const isMetaSticky = cell.column.columnDef.meta?.sticky === 'left'
                    const nativePinLeft = cell.column.getIsPinned() === 'left'
                    const nativePinRight = cell.column.getIsPinned() === 'right'
                    const minSize = cell.column.columnDef.minSize
                    const maxSize = cell.column.columnDef.maxSize
                    const isFixed =
                      minSize !== undefined && maxSize !== undefined && minSize === maxSize

                    return (
                      <TableCell
                        key={cell.id}
                        className={cn(
                          cell.column.columnDef.meta?.cellClassName,
                          cell.column.columnDef.meta?.align === 'center' && 'text-center',
                          cell.column.columnDef.meta?.align === 'right' && 'text-right',
                          isMetaSticky && 'sticky left-0 z-10 bg-background',
                          nativePinLeft &&
                            !isMetaSticky &&
                            'sticky z-10 bg-background shadow-[2px_0_4px_-2px_hsl(var(--border))]',
                          nativePinRight &&
                            'sticky z-10 bg-background shadow-[-2px_0_4px_-2px_hsl(var(--border))]',
                        )}
                        style={{
                          width: isFixed ? `${minSize}px` : `var(--col-${cell.column.id}-size)`,
                          minWidth: isFixed
                            ? `${minSize}px`
                            : `var(--col-${cell.column.id}-min-size, auto)`,
                          maxWidth: isFixed
                            ? `${maxSize}px`
                            : `var(--col-${cell.column.id}-max-size, auto)`,
                          // Centered cells: zero td padding — inner flex div handles centering symmetrically
                          ...(cell.column.columnDef.meta?.align === 'center' && { padding: 0 }),
                          ...(nativePinLeft &&
                            !isMetaSticky && {
                              left: `${cell.column.getStart('left')}px`,
                            }),
                          ...(nativePinRight && {
                            right: `${cell.column.getAfter('right')}px`,
                          }),
                        }}
                      >
                        {cell.column.columnDef.meta?.align === 'center' ? (
                          <div className="flex h-full w-full items-center justify-center">
                            {flexRender(cell.column.columnDef.cell, cell.getContext())}
                          </div>
                        ) : (
                          flexRender(cell.column.columnDef.cell, cell.getContext())
                        )}
                      </TableCell>
                    )
                  })}
                </TableRow>
              )
            })
          )}
        </TableBody>
      </UITable>
    </div>

      {/* Floating overlay following cursor during header drag */}
      <DragOverlay dropAnimation={null}>
        {activeLabel ? (
          <div className="flex items-center gap-1.5 rounded-md border border-primary/40 bg-background px-3 py-1.5 text-sm font-medium shadow-lg ring-1 ring-primary/20">
            <GripVertical className="h-3.5 w-3.5 text-primary/60" />
            <span className="truncate">{activeLabel}</span>
          </div>
        ) : null}
      </DragOverlay>
  </DndContext>
  )
}

export type { DataTableProps }
