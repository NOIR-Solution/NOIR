/**
 * Reusable column factory functions for TanStack Table.
 * Use createColumnHelper<T>() in page-level column files, then call these
 * helpers for the recurring patterns (select, actions, status badge, etc.).
 */
import type { ColumnDef, RowData } from '@tanstack/react-table'
import type { TFunction } from 'i18next'
import { EllipsisVertical } from 'lucide-react'
import { Checkbox } from '@uikit'
import { DataTableColumnHeader } from '@/uikit/data-table/DataTableColumnHeader'
import { Button } from '@/uikit/button/Button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuTrigger,
} from '@/uikit/dropdown-menu/DropdownMenu'

/**
 * Sticky actions dropdown column.
 * Always placed as the FIRST column (leftmost, sticky).
 * Size: 44px fixed — do not make it hideable or sortable.
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
  size: 44,
  minSize: 44,
  maxSize: 44,
  enableSorting: false,
  enableHiding: false,
  meta: { sticky: 'left' as const, align: 'center' as const },
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
          <EllipsisVertical className="h-4 w-4" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="start">
        {renderItems(row.original)}
      </DropdownMenuContent>
    </DropdownMenu>
  ),
})

/**
 * Select-all checkbox column.
 * Always placed as the second column (after actions).
 * Size: 40px fixed — do not make it hideable.
 */
export const createSelectColumn = <TData extends RowData>(): ColumnDef<TData, unknown> => ({
  id: 'select',
  size: 40,
  minSize: 40,
  maxSize: 40,
  enableSorting: false,
  enableHiding: false,
  meta: { align: 'center' as const },
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
 * Full audit columns (Created At, Creator, Modified At, Editor) for list pages.
 * Use this when the backend provides createdByName and modifiedByName.
 * Place as the LAST data columns in the columns array.
 */
export const createFullAuditColumns = <TData extends RowData>(
  t: TFunction,
  formatDateTime: (date: Date | string) => string,
): ColumnDef<TData, unknown>[] => [
  {
    id: 'createdAt',
    accessorFn: (row) => (row as Record<string, unknown>).createdAt,
    header: ({ column }) => <DataTableColumnHeader column={column} title={t('labels.createdAt', 'Created At')} />,
    meta: { label: t('labels.createdAt', 'Created At') },
    size: 160,
    cell: ({ getValue }) => {
      const value = getValue() as string | null | undefined
      return value ? (
        <span className="text-sm text-muted-foreground whitespace-nowrap">{formatDateTime(value)}</span>
      ) : (
        <span className="text-sm text-muted-foreground">—</span>
      )
    },
  } as ColumnDef<TData, unknown>,
  {
    id: 'createdBy',
    accessorFn: (row) => (row as Record<string, unknown>).createdByName,
    header: ({ column }) => <DataTableColumnHeader column={column} title={t('labels.creator', 'Creator')} />,
    meta: { label: t('labels.creator', 'Creator') },
    size: 140,
    cell: ({ getValue }) => {
      const name = getValue() as string | null | undefined
      return name ? (
        <span className="text-sm text-muted-foreground">{name}</span>
      ) : (
        <span className="text-sm text-muted-foreground">—</span>
      )
    },
  } as ColumnDef<TData, unknown>,
  {
    id: 'modifiedAt',
    accessorFn: (row) => (row as Record<string, unknown>).modifiedAt,
    header: ({ column }) => <DataTableColumnHeader column={column} title={t('labels.modifiedAt', 'Modified At')} />,
    meta: { label: t('labels.modifiedAt', 'Modified At'), defaultHidden: true },
    size: 160,
    cell: ({ getValue }) => {
      const value = getValue() as string | null | undefined
      return value ? (
        <span className="text-sm text-muted-foreground whitespace-nowrap">{formatDateTime(value)}</span>
      ) : (
        <span className="text-sm text-muted-foreground">—</span>
      )
    },
  } as ColumnDef<TData, unknown>,
  {
    id: 'modifiedBy',
    accessorFn: (row) => (row as Record<string, unknown>).modifiedByName,
    header: ({ column }) => <DataTableColumnHeader column={column} title={t('labels.editor', 'Editor')} />,
    meta: { label: t('labels.editor', 'Editor'), defaultHidden: true },
    size: 140,
    cell: ({ getValue }) => {
      const name = getValue() as string | null | undefined
      return name ? (
        <span className="text-sm text-muted-foreground">{name}</span>
      ) : (
        <span className="text-sm text-muted-foreground">—</span>
      )
    },
  } as ColumnDef<TData, unknown>,
]
