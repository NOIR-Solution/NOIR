import { useState, useCallback } from 'react'
import { type Column, type Table, type RowData } from '@tanstack/react-table'
import {
  DndContext,
  closestCenter,
  PointerSensor,
  KeyboardSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
} from '@dnd-kit/core'
import {
  SortableContext,
  sortableKeyboardCoordinates,
  useSortable,
  arrayMove,
  verticalListSortingStrategy,
} from '@dnd-kit/sortable'
import { CSS } from '@dnd-kit/utilities'
import { GripVertical, SlidersHorizontal, RotateCcw, ArrowUp, ArrowDown, Layers, Pin, PinOff } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { cn } from '@/lib/utils'
import { Button } from '../button/Button'
import { Popover, PopoverContent, PopoverTrigger } from '../popover/Popover'

// ─── Sortable column item ────────────────────────────────────────────────────

interface SortableColumnItemProps {
  columnId: string
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  column: Column<any, unknown>
  isDragEnabled: boolean
}

const SortableColumnItem = ({ columnId, column, isDragEnabled }: SortableColumnItemProps) => {
  const { t } = useTranslation('common')

  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
    id: columnId,
    disabled: !isDragEnabled,
  })

  const style: React.CSSProperties = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.5 : 1,
  }

  const sortState = column.getIsSorted()
  const label =
    column.columnDef.meta?.label ??
    (typeof column.columnDef.header === 'string' ? column.columnDef.header : column.id)

  return (
    <div
      ref={setNodeRef}
      style={style}
      className="flex items-center gap-1.5 rounded-sm px-2 py-1.5 hover:bg-accent"
    >
      {/* Drag handle */}
      {isDragEnabled ? (
        <button
          type="button"
          {...attributes}
          {...listeners}
          className="cursor-grab touch-none text-muted-foreground/40 hover:text-muted-foreground active:cursor-grabbing"
          aria-label={t('labels.dragToReorder', 'Drag to reorder')}
        >
          <GripVertical className="h-4 w-4" />
        </button>
      ) : (
        <div className="w-4" />
      )}

      {/* Visibility checkbox — LightCheckbox pattern (plain button, no Radix Presence) */}
      <button
        type="button"
        role="checkbox"
        aria-checked={column.getIsVisible()}
        className={cn(
          'flex h-4 w-4 shrink-0 cursor-pointer items-center justify-center rounded-sm border transition-colors',
          column.getIsVisible()
            ? 'border-primary bg-primary text-primary-foreground'
            : 'border-input hover:border-primary/50',
        )}
        onClick={() => column.toggleVisibility(!column.getIsVisible())}
        aria-label={
          column.getIsVisible()
            ? t('labels.hideColumn', 'Hide {{name}}', { name: label })
            : t('labels.showColumn', 'Show {{name}}', { name: label })
        }
      >
        {column.getIsVisible() && (
          <svg className="h-3 w-3" viewBox="0 0 12 12" fill="none" aria-hidden="true">
            <path
              d="M2 6L5 9L10 3"
              stroke="currentColor"
              strokeWidth="1.5"
              strokeLinecap="round"
              strokeLinejoin="round"
            />
          </svg>
        )}
      </button>

      {/* Column name */}
      <span
        className={cn(
          'flex-1 truncate text-sm',
          !column.getIsVisible() && 'text-muted-foreground',
        )}
      >
        {label}
      </span>

      {/* Pin toggle — cycles: unpinned → left → right → unpinned */}
      {column.getCanPin() && (() => {
        const pinState = column.getIsPinned() // false | 'left' | 'right'
        const pinLabel = pinState === 'left'
          ? t('dataTable.pinnedLeft', 'Pinned left — click to pin right')
          : pinState === 'right'
            ? t('dataTable.pinnedRight', 'Pinned right — click to unpin')
            : t('dataTable.pinColumn', 'Pin column left')
        return (
          <button
            type="button"
            onClick={(e) => {
              e.stopPropagation()
              if (!pinState) column.pin('left')
              else if (pinState === 'left') column.pin('right')
              else column.pin(false)
            }}
            className={cn(
              'cursor-pointer rounded p-0.5 transition-colors shrink-0',
              pinState
                ? 'text-primary hover:text-primary/80'
                : 'text-muted-foreground/30 hover:text-muted-foreground',
            )}
            aria-label={pinLabel}
          >
            {pinState ? (
              <span className="relative">
                <PinOff className="h-3.5 w-3.5" />
                <span className="absolute -top-1 -right-1.5 text-[8px] font-bold leading-none">
                  {pinState === 'left' ? 'L' : 'R'}
                </span>
              </span>
            ) : (
              <Pin className="h-3.5 w-3.5" />
            )}
          </button>
        )
      })()}

      {/* Sort controls (only for sortable columns) */}
      {column.getCanSort() && (
        <div className="flex items-center gap-0.5 shrink-0">
          <button
            type="button"
            onClick={(e) => {
              e.stopPropagation()
              if (sortState === 'asc') {
                column.clearSorting()
              } else {
                column.toggleSorting(false) // false = ascending
              }
            }}
            className={cn(
              'cursor-pointer rounded p-0.5 transition-colors',
              sortState === 'asc'
                ? 'text-primary hover:text-primary/80'
                : 'text-muted-foreground/30 hover:text-muted-foreground',
            )}
            aria-label={t('labels.sortAscending', 'Sort ascending')}
            aria-pressed={sortState === 'asc'}
          >
            <ArrowUp className="h-3.5 w-3.5" />
          </button>
          <button
            type="button"
            onClick={(e) => {
              e.stopPropagation()
              if (sortState === 'desc') {
                column.clearSorting()
              } else {
                column.toggleSorting(true) // true = descending
              }
            }}
            className={cn(
              'cursor-pointer rounded p-0.5 transition-colors',
              sortState === 'desc'
                ? 'text-primary hover:text-primary/80'
                : 'text-muted-foreground/30 hover:text-muted-foreground',
            )}
            aria-label={t('labels.sortDescending', 'Sort descending')}
            aria-pressed={sortState === 'desc'}
          >
            <ArrowDown className="h-3.5 w-3.5" />
          </button>
        </div>
      )}
    </div>
  )
}

// ─── Main dropdown component ─────────────────────────────────────────────────

interface DataTableColumnsDropdownProps<TData extends RowData> {
  table: Table<TData>
  /**
   * Column order array for drag-reorder support.
   * When provided alongside onColumnsReorder, enables drag handles.
   */
  columnOrder?: string[]
  /** Called when columns are reordered via drag. Update your state with the new order. */
  onColumnsReorder?: (newOrder: string[]) => void
  /**
   * True when any table setting differs from default (order/visibility/sizing).
   * Shows "Reset to default" button at the bottom.
   */
  isCustomized?: boolean
  /** Called when user clicks "Reset to default" (full settings reset). */
  onResetSettings?: () => void
  /** Column IDs available for grouping. When provided, shows a Group By section. */
  groupableColumnIds?: string[]
  /** Currently active grouping column IDs. */
  grouping?: string[]
  /** Called when grouping selection changes. */
  onGroupingChange?: (columnIds: string[]) => void
}

/**
 * Enhanced column management dropdown — replaces the basic Columns toggle.
 *
 * Features (when optional props are provided):
 * - Drag-to-reorder columns (requires columnOrder + onColumnsReorder)
 * - Sort controls per column (Asc/Desc toggle)
 * - Reset to default button (requires isCustomized + onResetSettings)
 *
 * Uses Popover (not DropdownMenu) to avoid Radix pointer-event conflicts with @dnd-kit.
 */
export const DataTableColumnsDropdown = <TData extends RowData>({
  table,
  columnOrder,
  onColumnsReorder,
  isCustomized = false,
  onResetSettings,
  groupableColumnIds,
  grouping = [],
  onGroupingChange,
}: DataTableColumnsDropdownProps<TData>) => {
  const { t } = useTranslation('common')
  const [open, setOpen] = useState(false)

  const isDragEnabled = !!onColumnsReorder && !!columnOrder

  // Only hideable columns appear in the panel
  const hideableColumns = table.getAllColumns().filter((col) => col.getCanHide())

  // Use provided order if available, else use default column order
  const orderedColumnIds = isDragEnabled
    ? (columnOrder ?? []).filter((id) => hideableColumns.some((c) => c.id === id))
    : hideableColumns.map((c) => c.id)

  const hasHiddenColumns = hideableColumns.some((col) => !col.getIsVisible())
  const showReset = isCustomized || hasHiddenColumns

  const sensors = useSensors(
    useSensor(PointerSensor, {
      activationConstraint: { distance: 5 },
    }),
    useSensor(KeyboardSensor, {
      coordinateGetter: sortableKeyboardCoordinates,
    }),
  )

  const handleDragEnd = useCallback(
    (event: DragEndEvent) => {
      const { active, over } = event
      if (!over || active.id === over.id || !columnOrder || !onColumnsReorder) return

      const oldIndex = columnOrder.indexOf(String(active.id))
      const newIndex = columnOrder.indexOf(String(over.id))
      if (oldIndex === -1 || newIndex === -1) return

      onColumnsReorder(arrayMove(columnOrder, oldIndex, newIndex))
    },
    [columnOrder, onColumnsReorder],
  )

  const handleReset = useCallback(() => {
    if (onResetSettings) {
      onResetSettings()
    }
    setOpen(false)
  }, [onResetSettings])

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          variant="outline"
          size="sm"
          className="h-9 cursor-pointer"
          aria-label={t('labels.toggleColumns', 'Toggle columns')}
        >
          <SlidersHorizontal className="mr-1.5 h-4 w-4" />
          {t('labels.columns', 'Columns')}
        </Button>
      </PopoverTrigger>

      <PopoverContent align="end" sideOffset={4} className="w-[220px] p-0">
        {/* Header */}
        <div className="border-b px-3 py-2">
          <p className="text-sm font-medium text-foreground">
            {t('labels.columns', 'Columns')}
          </p>
        </div>

        {/* Column list */}
        <div className="max-h-[300px] overflow-y-auto py-1">
          {isDragEnabled ? (
            <DndContext
              sensors={sensors}
              collisionDetection={closestCenter}
              onDragEnd={handleDragEnd}
            >
              <SortableContext items={orderedColumnIds} strategy={verticalListSortingStrategy}>
                {orderedColumnIds.map((columnId) => {
                  const column = table.getColumn(columnId)
                  if (!column) return null
                  return (
                    <SortableColumnItem
                      key={columnId}
                      columnId={columnId}
                      column={column}
                      isDragEnabled={true}
                    />
                  )
                })}
              </SortableContext>
            </DndContext>
          ) : (
            orderedColumnIds.map((columnId) => {
              const column = table.getColumn(columnId)
              if (!column) return null
              return (
                <SortableColumnItem
                  key={columnId}
                  columnId={columnId}
                  column={column}
                  isDragEnabled={false}
                />
              )
            })
          )}
        </div>

        {/* Group By section */}
        {groupableColumnIds && groupableColumnIds.length > 0 && onGroupingChange && (
          <>
            <div className="border-t" />
            <div className="px-3 py-2">
              <p className="flex items-center gap-1.5 text-xs font-medium text-muted-foreground">
                <Layers className="h-3.5 w-3.5" />
                {t('labels.groupBy', 'Group By')}
              </p>
            </div>
            <div className="pb-1">
              {groupableColumnIds.map((colId) => {
                const col = table.getColumn(colId)
                const label =
                  col?.columnDef.meta?.label ??
                  (typeof col?.columnDef.header === 'string' ? col.columnDef.header : colId)
                const isActive = grouping.includes(colId)
                return (
                  <button
                    key={colId}
                    type="button"
                    className={cn(
                      'flex w-full cursor-pointer items-center gap-2 rounded-sm px-3 py-1.5 text-sm transition-colors',
                      isActive
                        ? 'bg-primary/10 text-primary hover:bg-primary/15'
                        : 'text-foreground hover:bg-accent',
                    )}
                    onClick={() => {
                      if (isActive) {
                        onGroupingChange(grouping.filter((id) => id !== colId))
                      } else {
                        onGroupingChange([...grouping, colId])
                      }
                    }}
                    aria-pressed={isActive}
                  >
                    <Layers className={cn('h-3.5 w-3.5 shrink-0', isActive ? 'text-primary' : 'text-muted-foreground')} />
                    <span className="truncate">{label}</span>
                    {isActive && (
                      <span className="ml-auto text-xs font-medium text-primary">
                        {t('labels.active', 'On')}
                      </span>
                    )}
                  </button>
                )
              })}
            </div>
          </>
        )}

        {/* Reset button */}
        {showReset && (
          <>
            <div className="border-t" />
            <div className="py-1">
              <button
                type="button"
                className="flex w-full cursor-pointer items-center rounded-sm px-3 py-1.5 text-sm text-muted-foreground hover:bg-accent hover:text-accent-foreground"
                onClick={handleReset}
              >
                <RotateCcw className="mr-2 h-3.5 w-3.5" />
                {t('labels.resetToDefault', 'Reset to default')}
              </button>
            </div>
          </>
        )}
      </PopoverContent>
    </Popover>
  )
}
