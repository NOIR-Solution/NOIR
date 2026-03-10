/**
 * Reusable column factory functions for TanStack Table.
 * Use createColumnHelper<T>() in page-level column files, then call these
 * helpers for the recurring patterns (select, actions, status badge, etc.).
 */
import type { ColumnDef, RowData } from '@tanstack/react-table'
import { MoreHorizontal } from 'lucide-react'
import { Checkbox } from '@uikit'
import { Button } from '@/uikit/button/Button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuTrigger,
} from '@/uikit/dropdown-menu/DropdownMenu'

/**
 * Select-all checkbox column.
 * Always placed as the first column.
 * Size: 40px — do not make it hideable.
 */
export const createSelectColumn = <TData extends RowData>(): ColumnDef<TData, unknown> => ({
  id: 'select',
  size: 40,
  enableSorting: false,
  enableHiding: false,
  header: ({ table }) => (
    <Checkbox
      checked={
        table.getIsAllPageRowsSelected()
          ? true
          : table.getIsSomePageRowsSelected()
            ? 'indeterminate'
            : false
      }
      onCheckedChange={(value) => table.toggleAllPageRowsSelected(!!value)}
      aria-label="Select all rows on this page"
      className="cursor-pointer"
    />
  ),
  cell: ({ row }) => (
    <Checkbox
      checked={row.getIsSelected()}
      disabled={!row.getCanSelect()}
      onCheckedChange={(value) => row.toggleSelected(!!value)}
      onClick={(e) => e.stopPropagation()}
      aria-label="Select row"
      className="cursor-pointer"
    />
  ),
})

/**
 * Sticky actions dropdown column.
 * Always placed as the last column.
 * Size: 52px — do not make it hideable or sortable.
 *
 * @example
 * createActionsColumn<OrderSummaryDto>((row) => (
 *   <>
 *     <DropdownMenuItem onClick={() => handleView(row.id)}>View</DropdownMenuItem>
 *     <DropdownMenuItem onClick={() => handleEdit(row)}>Edit</DropdownMenuItem>
 *   </>
 * ))
 */
export const createActionsColumn = <TData extends RowData>(
  renderItems: (row: TData) => React.ReactNode,
): ColumnDef<TData, unknown> => ({
  id: 'actions',
  size: 52,
  enableSorting: false,
  enableHiding: false,
  cell: ({ row }) => (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          variant="ghost"
          size="icon"
          className="h-8 w-8 cursor-pointer"
          aria-label="Open row actions"
          onClick={(e) => e.stopPropagation()}
        >
          <MoreHorizontal className="h-4 w-4" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        {renderItems(row.original)}
      </DropdownMenuContent>
    </DropdownMenu>
  ),
})
