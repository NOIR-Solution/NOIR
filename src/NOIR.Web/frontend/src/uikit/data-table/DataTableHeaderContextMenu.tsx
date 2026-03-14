import type { Column, RowData } from '@tanstack/react-table'
import {
  ArrowUpNarrowWide,
  ArrowDownNarrowWide,
  ArrowLeftToLine,
  ArrowRightToLine,
  EyeOff,
  Pin,
  PinOff,
  RotateCcw,
} from 'lucide-react'
import { useTranslation } from 'react-i18next'
import {
  ContextMenu,
  ContextMenuTrigger,
  ContextMenuContent,
  ContextMenuItem,
  ContextMenuSeparator,
  ContextMenuSub,
  ContextMenuSubContent,
  ContextMenuSubTrigger,
} from '../context-menu/ContextMenu'

interface DataTableHeaderContextMenuProps<TData extends RowData> {
  column: Column<TData, unknown>
  children: React.ReactNode
}

/**
 * Right-click context menu for DataTable column headers.
 * Provides: Sort Asc/Desc, Pin Left/Right/Unpin, Hide Column, Reset Column Width.
 */
export const DataTableHeaderContextMenu = <TData extends RowData>({
  column,
  children,
}: DataTableHeaderContextMenuProps<TData>) => {
  const { t } = useTranslation('common')

  const canSort = column.getCanSort()
  const canHide = column.getCanHide()
  const canPin = column.getCanPin()
  const canResize = column.getCanResize()
  const isSortedAsc = column.getIsSorted() === 'asc'
  const isSortedDesc = column.getIsSorted() === 'desc'
  const isPinnedLeft = column.getIsPinned() === 'left'
  const isPinnedRight = column.getIsPinned() === 'right'
  const isPinned = isPinnedLeft || isPinnedRight

  // Skip context menu for fixed columns (actions, select) — nothing useful to do
  const minSize = column.columnDef.minSize
  const maxSize = column.columnDef.maxSize
  const isFixed = minSize !== undefined && maxSize !== undefined && minSize === maxSize
  if (isFixed) return <>{children}</>

  const hasAnyAction = canSort || canHide || canPin || canResize
  if (!hasAnyAction) return <>{children}</>

  return (
    <ContextMenu>
      <ContextMenuTrigger asChild>{children}</ContextMenuTrigger>
      <ContextMenuContent className="w-48">
        {/* Sort options */}
        {canSort && (
          <>
            <ContextMenuItem
              className="cursor-pointer gap-2"
              onClick={() => column.toggleSorting(false)}
              disabled={isSortedAsc}
            >
              <ArrowUpNarrowWide className="h-4 w-4" />
              {t('dataTable.sortAscending', 'Sort Ascending')}
            </ContextMenuItem>
            <ContextMenuItem
              className="cursor-pointer gap-2"
              onClick={() => column.toggleSorting(true)}
              disabled={isSortedDesc}
            >
              <ArrowDownNarrowWide className="h-4 w-4" />
              {t('dataTable.sortDescending', 'Sort Descending')}
            </ContextMenuItem>
            {(isSortedAsc || isSortedDesc) && (
              <ContextMenuItem
                className="cursor-pointer gap-2"
                onClick={() => column.clearSorting()}
              >
                <RotateCcw className="h-4 w-4" />
                {t('dataTable.clearSort', 'Clear Sort')}
              </ContextMenuItem>
            )}
          </>
        )}

        {/* Pin options */}
        {canPin && canSort && <ContextMenuSeparator />}
        {canPin && (
          <>
            {!isPinned ? (
              <ContextMenuSub>
                <ContextMenuSubTrigger className="cursor-pointer gap-2">
                  <Pin className="h-4 w-4" />
                  {t('dataTable.pinColumn', 'Pin Column')}
                </ContextMenuSubTrigger>
                <ContextMenuSubContent>
                  <ContextMenuItem
                    className="cursor-pointer gap-2"
                    onClick={() => column.pin('left')}
                  >
                    <ArrowLeftToLine className="h-4 w-4" />
                    {t('dataTable.pinLeft', 'Pin Left')}
                  </ContextMenuItem>
                  <ContextMenuItem
                    className="cursor-pointer gap-2"
                    onClick={() => column.pin('right')}
                  >
                    <ArrowRightToLine className="h-4 w-4" />
                    {t('dataTable.pinRight', 'Pin Right')}
                  </ContextMenuItem>
                </ContextMenuSubContent>
              </ContextMenuSub>
            ) : (
              <ContextMenuItem
                className="cursor-pointer gap-2"
                onClick={() => column.pin(false)}
              >
                <PinOff className="h-4 w-4" />
                {t('dataTable.unpinColumn', 'Unpin Column')}
              </ContextMenuItem>
            )}
          </>
        )}

        {/* Hide column */}
        {canHide && (canSort || canPin) && <ContextMenuSeparator />}
        {canHide && (
          <ContextMenuItem
            className="cursor-pointer gap-2"
            onClick={() => column.toggleVisibility(false)}
          >
            <EyeOff className="h-4 w-4" />
            {t('dataTable.hideColumn', 'Hide Column')}
          </ContextMenuItem>
        )}

        {/* Reset column width */}
        {canResize && (
          <ContextMenuItem
            className="cursor-pointer gap-2"
            onClick={() => column.resetSize()}
          >
            <RotateCcw className="h-4 w-4" />
            {t('dataTable.resetColumnWidth', 'Reset Column Width')}
          </ContextMenuItem>
        )}
      </ContextMenuContent>
    </ContextMenu>
  )
}
